using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Alpiste.Lib.PlcTag;
using Alpiste.Utils;
using Alpiste.Lib;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Linq;
using static Alpiste.Protocol.AB.CIP;
using System.Security.Cryptography;
using System.IO.IsolatedStorage;
//using System.Net.Http.Headers;
using System.ComponentModel.Design;
using System.Drawing;
using static libplctag.NativeImport.plctag;
using System.Diagnostics.Eventing.Reader;


namespace Alpiste.Protocol.AB
{
    public enum PlcType {
        AB_PLC_NONE = 0,
        AB_PLC_PLC5 = 1,
        AB_PLC_SLC,
        AB_PLC_MLGX,
        AB_PLC_LGX,
        AB_PLC_LGX_PCCC,
        AB_PLC_MICRO800,
        AB_PLC_OMRON_NJNX,
        AB_PLC_TYPE_LAST,
    }

    /* default string types used for ControlLogix-class PLCs. */
    public class logix_tag_byte_order: tag_byte_order_t  {
        public logix_tag_byte_order()
        {

            is_allocated = false;

            int16_order = new Int16[] { 0, 1 };
            int32_order = new Int16[] { 0, 1, 2, 3 };
            int64_order = new Int16[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            float32_order = new short[] { 0, 1, 2, 3 };
            float64_order = new Int16[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            str_is_defined = true;
            str_is_counted = true;
            str_is_fixed_length = true;
            str_is_zero_terminated = false;
            str_is_byte_swapped = false;

            str_pad_to_multiple_bytes = 1;
            str_count_word_bytes = 4;
            str_max_capacity = 82;
            str_total_length = 88;
            str_pad_bytes = 2;
        }

    };


    public class AbTag:PlcTag
    {

        /* vtables for different kinds of tags */
        /*      struct tag_vtable_t default_vtable = {
                    default_abort,
                    default_read,
                    default_status,
                    default_tickler,
                    default_write,
                    (tag_vtable_func) NULL, /* this is not portable! */

        /* attribute accessors */
        /*  ab_get_int_attrib,
            ab_set_int_attrib,

            ab_get_byte_array_attrib
};*/

        // Para Overridear
        override public int abort() { return 0; }
        override public int read() { return tag_read_start(); }
        override public int status() { return ab_tag_status(); }
        override public int tickler() { return tag_tickler(); }
        override public int write() { return tag_write_start(); }

        override public int wake_plc() { return 0; }
        


        int failed;
        int on_list;

        /* gateway connection related info */
        String host;
        int port;
        String path;
        private string gateway;
        Socket sock;

        /* connection variables. */
        protected bool use_connected_msg;
        bool only_use_old_forward_open;
        int fo_conn_size; /* old FO max connection size */
        int fo_ex_conn_size; /* extended FO max connection size */
        UInt16 max_payload_guess;
        UInt16 max_payload_size;

        UInt32 orig_connection_id;
        UInt32 targ_connection_id;
        UInt16 conn_seq_num;
        UInt16 conn_serial_number;

        PlcType plc_type;

        String conn_path;
        Byte conn_path_size;
        UInt16 dhp_dest;
        UInt16 is_dhp;

        UInt16 connection_group_id;

        /* registration info */
        UInt32 session_handle;

        /* Sequence ID for requests. */
        UInt64 session_seq_id;

        /* list of outstanding requests for this session */
      //  vector_p requests;

        UInt64 resp_seq_id;

        /* data for receiving messages */
        UInt32 data_offset;
        UInt32 data_capacity;
        UInt32 data_size;
        //Byte[] data;
        bool data_buffer_is_static;
        // uint8_t data[MAX_PACKET_SIZE_EX];

        UInt64 packet_count;

      //  thread_p handler_thread;
        volatile int terminating;
      //  mutex_p mutex;
      //  cond_p wait_cond;

        /* disconnect handling */
        UInt16 auto_disconnect_enabled;
        UInt16 auto_disconnect_timeout_ms;



           /* pointers back to session */
        public  /*WeakReference<*/Session session;
        //   int use_connected_msg;

        /* this contains the encoded name */
        public byte[] encoded_name = new byte[CIP.MAX_TAG_NAME];
        public int encoded_name_size;

        //    const char *read_group;

        /* storage for the encoded type. */
        public byte[] encoded_type_info; // o[MAX_TAG_TYPE_INFO];
        public int encoded_type_info_size;

        /* number of elements and size of each in the tag. */
     //   pccc_file_t file_type;
        public ElemType elem_type;

        public int elem_count;
        public int elem_size;

        public int special_tag;

        /* Used for standard tags. How much data can we send per packet? */
        public int write_data_per_packet;

        /* used for listing tags. */
        public UInt32 next_id;

        /* used for UDT tags. */
        public byte udt_get_fields;
        public UInt16 udt_id;

        /* requests */
        public int pre_write_read;
        public int first_read;
        public Request /*ab_request_p*/ req;
        public int offset;

        public int allow_packing;

        /* flags for operations */
        public int read_in_progress;
        public int write_in_progress;
        /*int connect_in_progress;*/

        public static AbTag AbTag_select_constructor(attr attribs, callback_func_ex tag_callback_func, Object userdata)
        {
            PlcType plc_type = get_plc_type(attribs);

            switch (plc_type)
            {
                case PlcType.AB_PLC_LGX:
                    return new EipCipTag(attribs, tag_callback_func, userdata);
                default:
                    //return new AbTag(attribs, tag_callback_func, userdata);
                    throw new PLCNotSupportedException();
            }
            ///* set up PLC-specific information. */
            //switch (plc_type)
            //{
            //    case PlcType.AB_PLC_PLC5:
            //        if (session.is_dhp == 0)
            //        {
            //            //pdebug(DEBUG_DETAIL, "Setting up PLC/5 tag.");

            //            if (path.Length != 0)
            //            {
            //                //pdebug(DEBUG_WARN, "A path is not supported for this PLC type if it is not for a DH+ bridge.");
            //            }

            //            use_connected_msg = false;
            //            //*HR*                     //tag->vtable = &plc5_vtable;
            //        }
            //        else
            //        {
            //            //pdebug(DEBUG_DETAIL, "Setting up PLC/5 via DH+ bridge tag.");
            //            use_connected_msg = true;
            //            //*HR*                    //tag->vtable = &eip_plc5_dhp_vtable;
            //        }

            //        //*HR*                tag.byte_order = &plc5_tag_byte_order;

            //        allow_packing = 0;
            //        break;

            //    case PlcType.AB_PLC_SLC:
            //    case PlcType.AB_PLC_MLGX:
            //        if (session.is_dhp == 0)
            //        {

            //            if (path.Length != 0)
            //            {
            //                //pdebug(DEBUG_WARN, "A path is not supported for this PLC type if it is not for a DH+ bridge.");
            //            }

            //            //pdebug(DEBUG_DETAIL, "Setting up SLC/MicroLogix tag.");
            //            use_connected_msg = false;
            //            //*HR*                    //tag->vtable = &slc_vtable;
            //        }
            //        else
            //        {
            //            //pdebug(DEBUG_DETAIL, "Setting up SLC/MicroLogix via DH+ bridge tag.");
            //            use_connected_msg = true;
            //            //*HR*                //tag->vtable = &eip_slc_dhp_vtable;
            //        }

            //        //*HR*                tag.byte_order = &slc_tag_byte_order;

            //        allow_packing = 0;
            //        break;

            //    case PlcType.AB_PLC_LGX_PCCC:
            //        //pdebug(DEBUG_DETAIL, "Setting up PCCC-mapped Logix tag.");
            //        use_connected_msg = false;
            //        allow_packing = 0;
            //        //tag->vtable = &lgx_pccc_vtable;

            //        //*HR*                tag.byte_order = &slc_tag_byte_order;

            //        break;

            //    case PlcType.AB_PLC_LGX:
            //        //pdebug(DEBUG_DETAIL, "Setting up Logix tag.");

            //        /* Logix tags need a path. */
            //        if (path == null && plc_type == PlcType.AB_PLC_LGX)
            //        {
            //            //pdebug(DEBUG_WARN, "A path is required for Logix-class PLCs!");
            //            status = PLCTAG_ERR_BAD_PARAM;
            //            throw new Exception("Se necesita un path");
            //            //return tag;
            //        }

            //        /* if we did not fill in the byte order elsewhere, fill it in now. */
            //        if (byte_order == null)
            //        {
            //            //pdebug(DEBUG_DETAIL, "Using default Logix byte order.");
            //            byte_order = new logix_tag_byte_order();
            //        }

            //        /* if this was not filled in elsewhere default to Logix */
            //        /*HR*                if (tag->vtable == &default_vtable || !tag->vtable)
            //                        {
            //                            //pdebug(DEBUG_DETAIL, "Setting default Logix vtable.");
            //                            tag->vtable = &eip_cip_vtable;
            //                        }
            //        */
            //        /* default to requiring a connection. */
            //        use_connected_msg = Attr.attr_get_int(attribs, "use_connected_msg", 1) != 0;
            //        allow_packing = Attr.attr_get_int(attribs, "allow_packing", 1);

            //        break;

            //    case PlcType.AB_PLC_MICRO800:
            //        //pdebug(DEBUG_DETAIL, "Setting up Micro8X0 tag.");

            //        if (path != null || path.Length != 0)
            //        {
            //            //pdebug(DEBUG_WARN, "A path is not supported for this PLC type.");
            //        }

            //        /* if we did not fill in the byte order elsewhere, fill it in now. */
            //        if (byte_order == null)
            //        {
            //            //pdebug(DEBUG_DETAIL, "Using default Micro8x0 byte order.");
            //            byte_order = new logix_tag_byte_order();
            //        }

            //        /* if this was not filled in elsewhere default to generic *Logix */
            //        /*HR*               if (tag->vtable == &default_vtable || !tag->vtable)
            //                       {
            //                           //pdebug(DEBUG_DETAIL, "Setting default Logix vtable.");
            //                           tag->vtable = &eip_cip_vtable;
            //                       }
            //        */
            //        use_connected_msg = true;
            //        allow_packing = 0;

            //        break;

            //    // case AB_PLC_OMRON_NJNX:
            //    //     pdebug(DEBUG_DETAIL, "Setting up OMRON NJ/NX Series tag.");

            //    //     if(str_length(path) == 0) {
            //    //         pdebug(DEBUG_WARN,"A path is required for this PLC type.");
            //    //         tag->status = PLCTAG_ERR_BAD_PARAM;
            //    //         return (plc_tag_p)tag;
            //    //     }

            //    //     /* if we did not fill in the byte order elsewhere, fill it in now. */
            //    //     if(!tag->byte_order) {
            //    //         pdebug(DEBUG_DETAIL, "Using default Omron byte order.");
            //    //         tag->byte_order = &omron_njnx_tag_byte_order;
            //    //     }

            //    //     /* if this was not filled in elsewhere default to generic *Logix */
            //    //     if(tag->vtable == &default_vtable || !tag->vtable) {
            //    //         pdebug(DEBUG_DETAIL, "Setting default Logix vtable.");
            //    //         tag->vtable = &eip_cip_vtable;
            //    //     }

            //    //     tag->use_connected_msg = 1;
            //    //     tag->allow_packing = attr_get_int(attribs, "allow_packing", 0);

            //    //     break;

            //    default:
            //        //pdebug(DEBUG_WARN, "Unknown PLC type!");
            //        status = PLCTAG_ERR_BAD_CONFIG;
            //        throw new Exception("PLC desconocido");
            //        //return tag;
            //}

        }
        protected AbTag(attr attribs, callback_func_ex tag_callback_func = null, Object userdata = null) : base(attribs, tag_callback_func, userdata)
        {
            /*
 * check the CPU type.
 *
 * This determines the protocol type.
 */
            PlcType plctype = get_plc_type(attribs);

            switch(plctype)
            {

            }

            if (check_cpu(attribs) != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_WARN, "CPU type not valid or missing.");
                /* tag->status = PLCTAG_ERR_BAD_DEVICE; */
                //rc_dec(tag);
                throw new Exception("CPU type not valid or missing");
                //return null; // (plc_tag_p)NULL;
            }

            /* set up any required settings based on the cpu type. */
            switch (plc_type)
            {
                case PlcType.AB_PLC_LGX_PCCC:
                case PlcType.AB_PLC_PLC5:
                case PlcType.AB_PLC_SLC:
                case PlcType.AB_PLC_MLGX:
                    use_connected_msg = false;
                    allow_packing = 0;
                    break;

                case PlcType.AB_PLC_LGX:
                    /* default to requiring a connection and allowing packing. */
                    use_connected_msg = Attr.attr_get_int(attribs, "use_connected_msg", 1) != 0;
                    allow_packing = Attr.attr_get_int(attribs, "allow_packing", 1);
                    break;

                case PlcType.AB_PLC_MICRO800:
                    /* we must use connected messaging here. */
                    //pdebug(DEBUG_DETAIL, "Micro800 needs connected messaging.");
                    use_connected_msg = true;

                    /* Micro800 cannot pack requests. */
                    allow_packing = 0;
                    break;

                // case AB_PLC_OMRON_NJNX:
                //     tag->use_connected_msg = 1;

                //     /*
                //      * Default packing to off.  Omron requires the client to do the calculation of
                //      * whether the results will fit or not.
                //      */
                //     tag->allow_packing = attr_get_int(attribs, "allow_packing", 0);
                //     break;

                default:
                    //pdebug(DEBUG_WARN, "Unknown PLC type!");
                    status_ = PLCTAG_ERR_BAD_CONFIG;
                    throw new Exception("Error en la configuración");
                    //return tag;
                    break;
            }

            /* make sure that the connection requirement is forced. */
            Attr.attr_set_int(attribs, "use_connected_msg", use_connected_msg ? 1 : 0);

            /* get the connection path.  We need this to make a decision about the PLC. */
            path = Attr.attr_get_str(attribs, "path", null);

            /*
             * Find or create a session.
             *
             * All tags need sessions.  They are the TCP connection to the gateway PLC.
             */
            session = Session.session_find_or_create(/*ref session,*/ attribs); //.weakReference;
            /*if (Session.session_find_or_create(ref session, attribs) != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_INFO, "Unable to create session!");
                status = PLCTAG_ERR_BAD_GATEWAY;
                throw new Exception("No se puede crear sesión");
                //return tag;
            }*/
            //WeakReference<PlcTag> weakTag = new WeakReference<PlcTag>(this);
            //Session session_;
            //session.TryGetTarget(out session_ );
            session.tags_references.Add(this/*.weakTag*/);


            //pdebug(DEBUG_DETAIL, "using session=%p", tag->session);

            int rc;
            /* get the tag data type, or try. */
            rc = get_tag_data_type(attribs);
            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_WARN, "Error %s getting tag element data type or handling special tag!", plc_tag_decode_error(rc));
                status_ = (byte)rc;
                throw new Exception("Error en obtener el tipo de dato");
                //return tag;
            }

            /* set up PLC-specific information. */
            switch (plc_type)
            {
                case PlcType.AB_PLC_PLC5:
                    if (session.is_dhp == 0)
                    {
                        //pdebug(DEBUG_DETAIL, "Setting up PLC/5 tag.");

                        if (path.Length != 0)
                        {
                            //pdebug(DEBUG_WARN, "A path is not supported for this PLC type if it is not for a DH+ bridge.");
                        }

                        use_connected_msg = false;
                        //*HR*                     //tag->vtable = &plc5_vtable;
                    }
                    else
                    {
                        //pdebug(DEBUG_DETAIL, "Setting up PLC/5 via DH+ bridge tag.");
                        use_connected_msg = true;
                        //*HR*                    //tag->vtable = &eip_plc5_dhp_vtable;
                    }

                    //*HR*                tag.byte_order = &plc5_tag_byte_order;

                    allow_packing = 0;
                    break;

                case PlcType.AB_PLC_SLC:
                case PlcType.AB_PLC_MLGX:
                    if (session.is_dhp == 0)
                    {

                        if (path.Length != 0)
                        {
                            //pdebug(DEBUG_WARN, "A path is not supported for this PLC type if it is not for a DH+ bridge.");
                        }

                        //pdebug(DEBUG_DETAIL, "Setting up SLC/MicroLogix tag.");
                        use_connected_msg = false;
                        //*HR*                    //tag->vtable = &slc_vtable;
                    }
                    else
                    {
                        //pdebug(DEBUG_DETAIL, "Setting up SLC/MicroLogix via DH+ bridge tag.");
                        use_connected_msg = true;
                        //*HR*                //tag->vtable = &eip_slc_dhp_vtable;
                    }

                    //*HR*                tag.byte_order = &slc_tag_byte_order;

                    allow_packing = 0;
                    break;

                case PlcType.AB_PLC_LGX_PCCC:
                    //pdebug(DEBUG_DETAIL, "Setting up PCCC-mapped Logix tag.");
                    use_connected_msg = false;
                    allow_packing = 0;
                    //tag->vtable = &lgx_pccc_vtable;

                    //*HR*                tag.byte_order = &slc_tag_byte_order;

                    break;

                case PlcType.AB_PLC_LGX:
                    //pdebug(DEBUG_DETAIL, "Setting up Logix tag.");

                    /* Logix tags need a path. */
                    if (path == null && plc_type == PlcType.AB_PLC_LGX)
                    {
                        //pdebug(DEBUG_WARN, "A path is required for Logix-class PLCs!");
                        status_ = PLCTAG_ERR_BAD_PARAM;
                        throw new Exception("Se necesita un path");
                        //return tag;
                    }

                    /* if we did not fill in the byte order elsewhere, fill it in now. */
                    if (byte_order == null)
                    {
                        //pdebug(DEBUG_DETAIL, "Using default Logix byte order.");
                        byte_order = new logix_tag_byte_order();
                    }

                    /* if this was not filled in elsewhere default to Logix */
                    /*HR*                if (tag->vtable == &default_vtable || !tag->vtable)
                                    {
                                        //pdebug(DEBUG_DETAIL, "Setting default Logix vtable.");
                                        tag->vtable = &eip_cip_vtable;
                                    }
                    */
                    /* default to requiring a connection. */
                    use_connected_msg = Attr.attr_get_int(attribs, "use_connected_msg", 1) != 0;
                    allow_packing = Attr.attr_get_int(attribs, "allow_packing", 1);

                    break;

                case PlcType.AB_PLC_MICRO800:
                    //pdebug(DEBUG_DETAIL, "Setting up Micro8X0 tag.");

                    if (path != null || path.Length != 0)
                    {
                        //pdebug(DEBUG_WARN, "A path is not supported for this PLC type.");
                    }

                    /* if we did not fill in the byte order elsewhere, fill it in now. */
                    if (byte_order == null)
                    {
                        //pdebug(DEBUG_DETAIL, "Using default Micro8x0 byte order.");
                        byte_order = new logix_tag_byte_order();
                    }

                    /* if this was not filled in elsewhere default to generic *Logix */
                    /*HR*               if (tag->vtable == &default_vtable || !tag->vtable)
                                   {
                                       //pdebug(DEBUG_DETAIL, "Setting default Logix vtable.");
                                       tag->vtable = &eip_cip_vtable;
                                   }
                    */
                    use_connected_msg = true;
                    allow_packing = 0;

                    break;

                // case AB_PLC_OMRON_NJNX:
                //     pdebug(DEBUG_DETAIL, "Setting up OMRON NJ/NX Series tag.");

                //     if(str_length(path) == 0) {
                //         pdebug(DEBUG_WARN,"A path is required for this PLC type.");
                //         tag->status = PLCTAG_ERR_BAD_PARAM;
                //         return (plc_tag_p)tag;
                //     }

                //     /* if we did not fill in the byte order elsewhere, fill it in now. */
                //     if(!tag->byte_order) {
                //         pdebug(DEBUG_DETAIL, "Using default Omron byte order.");
                //         tag->byte_order = &omron_njnx_tag_byte_order;
                //     }

                //     /* if this was not filled in elsewhere default to generic *Logix */
                //     if(tag->vtable == &default_vtable || !tag->vtable) {
                //         pdebug(DEBUG_DETAIL, "Setting default Logix vtable.");
                //         tag->vtable = &eip_cip_vtable;
                //     }

                //     tag->use_connected_msg = 1;
                //     tag->allow_packing = attr_get_int(attribs, "allow_packing", 0);

                //     break;

                default:
                    //pdebug(DEBUG_WARN, "Unknown PLC type!");
                    status_ = PLCTAG_ERR_BAD_CONFIG;
                    throw new Exception("PLC desconocido");
                    //return tag;
            }

            /* pass the connection requirement since it may be overridden above. */
            Attr.attr_set_int(attribs, "use_connected_msg", use_connected_msg ? 1 : 0);

            /* get the element count, default to 1 if missing. */
            elem_count = Attr.attr_get_int(attribs, "elem_count", 1);

            switch (plc_type)
            {
                // case AB_PLC_OMRON_NJNX:
                case PlcType.AB_PLC_LGX:
                /* fall through */
                case PlcType.AB_PLC_MICRO800:
                    /* fill this in when we read the tag. */
                    //tag->elem_size = 0;
                    size = 0;
                    data = null;
                    break;

                default:
                    /* we still need size on non Logix-class PLCs */
                    /* get the element size if it is not already set. */
                    if (elem_size == 0)
                    {
                        elem_size = Attr.attr_get_int(attribs, "elem_size", 0);
                    }

                    /* Determine the tag size */
                    size = (elem_count) * (elem_size);
                    if (size == 0)
                    {
                        /* failure! Need data_size! */
                        //pdebug(DEBUG_WARN, "Tag size is zero!");
                        status_ = PlcTag.PLCTAG_ERR_BAD_PARAM;
                        throw new Exception("El tamaño del tag es cero");
                        //return tag;
                    }

                    /* this may be changed in the future if this is a tag list request. */
                    //   tag->data = (uint8_t*)mem_alloc(tag->size);
                    data = new byte[size];

                    /*if (data == null)
                    {
                        //pdebug(DEBUG_WARN, "Unable to allocate tag data!");
                        status = PlcTag.PLCTAG_ERR_NO_MEM;
                        throw new Exception("");
                        //return tag;
                    }*/
                    break;
            }

            /*
             * check the tag name, this is protocol specific.
             */

            if (special_tag == 0 && check_tag_name(/*tag,*/ Attr.attr_get_str(attribs, "name", null)) != PlcTag.PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_INFO, "Bad tag name!");
                status_ = PLCTAG_ERR_BAD_PARAM;
                throw new Exception("Nombre de tag erroneo");


                //return tag;
            }

            /* kick off a read to get the tag type and size. */
            if (special_tag == 0 /*&& tag->vtable->read*/)
            {
                /* trigger the first read. */
                //pdebug(DEBUG_DETAIL, "Kicking off initial read.");

                first_read = 1;
                read_in_flight = true;
                /*.path->vtable->*/read(/*(plc_tag_p)tag*/);
                //       Thread.Sleep(1000);
                //*HR*?                 tag_raise_event((plc_tag_p)tag, PLCTAG_EVENT_READ_STARTED, tag->status);
            }
            else
            {
                //pdebug(DEBUG_DETAIL, "Not kicking off initial read: tag is special or does not have read function.");

                /* force the created event because we do not do an initial read here. */
                //*HR*                    tag_raise_event((plc_tag_p)tag, PLCTAG_EVENT_CREATED, tag->status);
            }
        }

        public virtual Session create_session_unsafe()
        {
            int use_connected_msg = this.use_connected_msg ? 1 : 0;
            switch (plc_type)
            {
                case PlcType.AB_PLC_PLC5:
                    //            session = create_plc5_session_unsafe(session_gw, session_path, &use_connected_msg, connection_group_id);
                    break;

                case PlcType.AB_PLC_SLC:
                    //            session = create_slc_session_unsafe(session_gw, session_path, &use_connected_msg, connection_group_id);
                    break;

                case PlcType.AB_PLC_MLGX:
                    //            session = create_mlgx_session_unsafe(session_gw, session_path, &use_connected_msg, connection_group_id);
                    break;

                case PlcType.AB_PLC_LGX:
                    session = Session.create_lgx_session_unsafe(gateway, path, ref use_connected_msg, connection_group_id);
                    break;

                case PlcType.AB_PLC_LGX_PCCC:
                    //            session = create_lgx_pccc_session_unsafe(session_gw, session_path, &use_connected_msg, connection_group_id);
                    break;

                case PlcType.AB_PLC_MICRO800:
                    //            session = create_micro800_session_unsafe(session_gw, session_path, &use_connected_msg, connection_group_id);
                    break;

                // case AB_PLC_OMRON_NJNX:
                //     session = create_omron_njnx_session_unsafe(session_gw, session_path, &use_connected_msg, connection_group_id);
                //     break;

                default:
                    //pdebug(DEBUG_WARN, "Unknown PLC type %d!", plc_type);
                    session = null;
                    break;
            }
            this.use_connected_msg = use_connected_msg != 0;
            return session;
        }
        public  AbTag(string name, string gateway, PlcType plcType = PlcType.AB_PLC_LGX,
            bool useConnectedMsg = true,
            bool allowPacking = true,
            string path = "1,0",
            bool shareSession = true,
            int connectionGroupId = 0,
            bool onlyUseOldForwaredOpen = false,
            int autoDisconnectMs = -1,
            string elemType = "dint",
            int elemSize = 2,
            int elemCount = 1,
            int timeout = 10000,
            Object userData = null)
            : base(/*timeout,  connectionGroupId,*/ null, null, userData)
        {
            /*
 * check the CPU type.
 *
 * This determines the protocol type.
 */
            
            plc_type = plcType;
            use_connected_msg = useConnectedMsg;
            allow_packing = allowPacking ? 1 : 0;
            this.path = path;
            this.gateway = gateway;
            
            

            //Session session = null; // AB_SESSION_NULL;
            int new_session = 0;
            int shared_session = shareSession ? 1 : 0;
            int auto_disconnect_enabled = 0;
            int auto_disconnect_timeout_ms = autoDisconnectMs;
            int only_use_old_forward_open = onlyUseOldForwaredOpen? 1 : 0;

            if (auto_disconnect_timeout_ms >= 0) 
            {
                auto_disconnect_enabled = 1;
            }
            
            
            lock (Session.session_mutex)
            {
                /* if we are to share sessions, then look for an existing one. */
                if (shareSession)
                {
                    session = Session.find_session_by_host_unsafe(gateway, path, connection_group_id);
                }
                else
                {
                    /* no sharing, create a new one */
                    session = null; // AB_SESSION_NULL;
                }

                
                if (session == null /*AB_SESSION_NULL*/)
                {

                    session = create_session_unsafe();


                    if (session == null /*AB_SESSION_NULL*/)
                    {
                        //pdebug(DEBUG_WARN, "unable to create or find a session!");
                        throw new Exception("unable to create or find a session!");
                        
                    }
                    else
                    {
                        session.auto_disconnect_enabled = auto_disconnect_enabled;
                        session.auto_disconnect_timeout_ms = auto_disconnect_timeout_ms;

                        /* see if we have an attribute set for forcing the use of the older ForwardOpen */
                        //pdebug(DEBUG_DETAIL, "Passed attribute to prohibit use of extended ForwardOpen is %d.", only_use_old_forward_open);
                        //pdebug(DEBUG_DETAIL, "Existing attribute to prohibit use of extended ForwardOpen is %d.", session->only_use_old_forward_open);
                        session.only_use_old_forward_open = (session.only_use_old_forward_open ? true : only_use_old_forward_open != 0);

                        new_session = 1;
                    }
                }
                else
                {
                    /* turn on auto disconnect if we need to. */
                    if (!(session.auto_disconnect_enabled != 0) && (auto_disconnect_enabled != 0))
                    {
                        session.auto_disconnect_enabled = auto_disconnect_enabled;
                    }

                    /* disconnect period always goes down. */
                    if (session.auto_disconnect_enabled != 0 && session.auto_disconnect_timeout_ms > auto_disconnect_timeout_ms)
                    {
                        session.auto_disconnect_timeout_ms = auto_disconnect_timeout_ms;
                    }

                    //pdebug(DEBUG_DETAIL, "Reusing existing session.");
                }
            }

            /*
             * do this OUTSIDE the mutex in order to let other threads not block if
             * the session creation process blocks.
             */

            
            if (new_session != 0)
            {
                if (session.session_init(/*session*/)!= PLCTAG_STATUS_OK)
                {
                    //rc_dec(session);
                    session = null; //AB_SESSION_NULL;
                }
                else
                {
                    /* save the status */
                    //session->status = rc;
                }
            }

            if (session == null)
            {
                //pdebug(DEBUG_INFO, "Unable to create session!");
                //status = PLCTAG_ERR_BAD_GATEWAY;
                throw new Exception("No se puede crear sesión");
                //return tag;
            }
            //WeakReference<PlcTag> weakTag = new WeakReference<PlcTag>(this);
            //Session session_;
            //session.TryGetTarget(out session_ );
            session.tags_references.Add(this/*.weakTag*/);


            //pdebug(DEBUG_DETAIL, "using session=%p", tag->session);

            int rc = PLCTAG_STATUS_OK;
            String elem_type = null;
            String tag_name = null;
    //*HR*           pccc_addr_t file_addr = { 0 };

            //pdebug(DEBUG_DETAIL, "Starting.");

            switch (plc_type)
            {
                case PlcType.AB_PLC_PLC5:
                case PlcType.AB_PLC_SLC:
                case PlcType.AB_PLC_LGX_PCCC:
                case PlcType.AB_PLC_MLGX:
                /*HR*                   tag_name = Attr.attr_get_str(attribs, "name", null);

                                   rc = parse_pccc_logical_address(tag_name, &file_addr);
                                   if (rc != PLCTAG_STATUS_OK)
                                   {
                                       pdebug(DEBUG_WARN, "Unable to parse data file %s!", tag_name);
                                       return rc;
                                   }

                                   tag->elem_size = file_addr.element_size_bytes;
                                   tag->file_type = (int)file_addr.file_type;

                                   break;
               */
                case PlcType.AB_PLC_LGX:
                case PlcType.AB_PLC_MICRO800:
                    // case AB_PLC_OMRON_NJNX:
                    /* look for the elem_type attribute. */
                    elem_type = elemType; //Attr.attr_get_str(attribs, "elem_type", null);
                    if (elem_type != null)
                    {
                        if (elem_type.ToLower() == "lint" || elem_type.ToLower() == "ulint")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of 64-bit integer.");
                            this.elem_size = 8;
                            this.elem_type = ElemType.AB_TYPE_INT64;
                        }
                        else if (elem_type.ToLower() == "dint" || elem_type.ToLower() == "udint")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of 32-bit integer.");
                            elem_size = 4;
                            this.elem_type = ElemType.AB_TYPE_INT32;
                        }
                        else if (elem_type.ToLower() == "int" || elem_type.ToLower() == "uint")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of 16-bit integer.");
                            elem_size = 2;
                            this.elem_type = ElemType.AB_TYPE_INT16;
                        }
                        else if (elem_type.ToLower() == "sint" || elem_type.ToLower() == "usint")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of 8-bit integer.");
                            elem_size = 1;
                            this.elem_type = ElemType.AB_TYPE_INT8;
                        }
                        else if (elem_type.ToLower() == "bool")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of bit.");
                            elem_size = 1;
                            this.elem_type = ElemType.AB_TYPE_BOOL;
                        }
                        else if (elem_type.ToLower() == "bool array")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of bool array.");
                            elem_size = 4;
                            this.elem_type = ElemType.AB_TYPE_BOOL_ARRAY;
                        }
                        else if (elem_type.ToLower() == "real")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of 32-bit float.");
                            elem_size = 4;
                            this.elem_type = ElemType.AB_TYPE_FLOAT32;
                        }
                        else if (elem_type.ToLower() == "lreal")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of 64-bit float.");
                            elem_size = 8;
                            this.elem_type = ElemType.AB_TYPE_FLOAT64;
                        }
                        else if (elem_type.ToLower() == "string")
                        {
                            //pdebug(DEBUG_DETAIL, "Fount tag element type of string.");
                            this.elem_size = 88;
                            this.elem_type = ElemType.AB_TYPE_STRING;
                        }
                        else if (elem_type.ToLower() == "short string")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of short string.");
                            this.elem_size = 256; /* TODO - find the real length */
                            this.elem_type = ElemType.AB_TYPE_SHORT_STRING;
                        }
                        else
                        {
                            //pdebug(DEBUG_DETAIL, "Unknown tag type %s", elem_type);
                            throw new Exception("Unknown tag type!");
                            //return PlcTag.PLCTAG_ERR_UNSUPPORTED;
                        }
                    }
                    else
                    {
                        /*
                         * We have two cases
                         *      * tag listing, but only for CIP PLCs (but not for UDTs!).
                         *      * no type, just elem_size.
                         * Otherwise this is an error.
                         */
                        int elem_size = elemSize; // Attr.attr_get_int(attribs, "elem_size", 0);
                        int cip_plc = !!(this.plc_type == PlcType.AB_PLC_LGX || this.plc_type == PlcType.AB_PLC_MICRO800 /*|| tag->plc_type == AB_PLC_OMRON_NJNX*/) ? 1 : 0;

                        if (cip_plc != 0)
                        {
                            String tmp_tag_name = name; // Attr.attr_get_str(attribs, "name", null);
                            int special_tag_rc = PlcTag.PLCTAG_STATUS_OK;

                            /* check for special tags. */
                            /*HR*                       if (tmp_tag_name.ToLower() == "@raw")
                                                   {
                                                       special_tag_rc = setup_raw_tag(tag);
                                                   }
                                                   else if (tmp_tag_name.ToLower()!= "@tags")
                                                   {
                                                       special_tag_rc = setup_tag_listing_tag(tag, tmp_tag_name);
                                                   }
                                                   else if (Str.str_str_cmp_i(tmp_tag_name, "@udt/")!=null)
                                                   {
                                                       special_tag_rc = setup_udt_tag(tag, tmp_tag_name);
                                                   } /* else not a special tag. */

                            /*HR*                        if (special_tag_rc != PLCTAG_STATUS_OK)
                                                    {
                                                        //pdebug(DEBUG_WARN, "Error parsing tag listing name!");
                                                        return special_tag_rc;
                                                    }
                            */
                        }

                        /* if we did not set an element size yet, set one. */
                        if (this.elem_size == 0)
                        {
                            if (elem_size > 0)
                            {
                                //pdebug(DEBUG_INFO, "Setting element size to %d.", elem_size);
                                this.elem_size = elem_size;
                            }
                        }
                        else
                        {
                            if (elem_size > 0)
                            {
                                //pdebug(DEBUG_WARN, "Tag has elem_size and either is a tag listing or has elem_type, only use one!");
                            }
                        }
                    }

                    break;

                default:
                    throw new Exception("Unknown PLC type!");
                    //pdebug(DEBUG_WARN, "Unknown PLC type!");
                    //return PLCTAG_ERR_BAD_DEVICE;
                    break;
            }

            /* set up PLC-specific information. */
            switch (plc_type)
            {
                case PlcType.AB_PLC_PLC5:
                    if (session.is_dhp == 0)
                    {
                        //pdebug(DEBUG_DETAIL, "Setting up PLC/5 tag.");

                        if (path.Length != 0)
                        {
                            //pdebug(DEBUG_WARN, "A path is not supported for this PLC type if it is not for a DH+ bridge.");
                        }

                        use_connected_msg = false;
                        //*HR*                     //tag->vtable = &plc5_vtable;
                    }
                    else
                    {
                        //pdebug(DEBUG_DETAIL, "Setting up PLC/5 via DH+ bridge tag.");
                        use_connected_msg = true;
                        //*HR*                    //tag->vtable = &eip_plc5_dhp_vtable;
                    }

                    //*HR*                tag.byte_order = &plc5_tag_byte_order;

                    allow_packing = 0;
                    break;

                case PlcType.AB_PLC_SLC:
                case PlcType.AB_PLC_MLGX:
                    if (session.is_dhp == 0)
                    {

                        if (path.Length != 0)
                        {
                            //pdebug(DEBUG_WARN, "A path is not supported for this PLC type if it is not for a DH+ bridge.");
                        }

                        //pdebug(DEBUG_DETAIL, "Setting up SLC/MicroLogix tag.");
                        use_connected_msg = false;
                        //*HR*                    //tag->vtable = &slc_vtable;
                    }
                    else
                    {
                        //pdebug(DEBUG_DETAIL, "Setting up SLC/MicroLogix via DH+ bridge tag.");
                        use_connected_msg = true;
                        //*HR*                //tag->vtable = &eip_slc_dhp_vtable;
                    }

                    //*HR*                tag.byte_order = &slc_tag_byte_order;

                    allow_packing = 0;
                    break;

                case PlcType.AB_PLC_LGX_PCCC:
                    //pdebug(DEBUG_DETAIL, "Setting up PCCC-mapped Logix tag.");
                    use_connected_msg = false;
                    allow_packing = 0;
                    //tag->vtable = &lgx_pccc_vtable;

                    //*HR*                tag.byte_order = &slc_tag_byte_order;

                    break;

                case PlcType.AB_PLC_LGX:
                    //pdebug(DEBUG_DETAIL, "Setting up Logix tag.");

                    /* Logix tags need a path. */
                    if (path == null && plc_type == PlcType.AB_PLC_LGX)
                    {
                        //pdebug(DEBUG_WARN, "A path is required for Logix-class PLCs!");
                        status_ = PLCTAG_ERR_BAD_PARAM;
                        throw new Exception("Se necesita un path");
                        //return tag;
                    }

                    /* if we did not fill in the byte order elsewhere, fill it in now. */
                    if (byte_order == null)
                    {
                        //pdebug(DEBUG_DETAIL, "Using default Logix byte order.");
                        byte_order = new logix_tag_byte_order();
                    }

                    /* if this was not filled in elsewhere default to Logix */
                    /*HR*                if (tag->vtable == &default_vtable || !tag->vtable)
                                    {
                                        //pdebug(DEBUG_DETAIL, "Setting default Logix vtable.");
                                        tag->vtable = &eip_cip_vtable;
                                    }
                    */
                    /* default to requiring a connection. */
                    //use_connected_msg = Attr.attr_get_int(attribs, "use_connected_msg", 1) != 0;
                    //allow_packing = Attr.attr_get_int(attribs, "allow_packing", 1);

                    break;

                case PlcType.AB_PLC_MICRO800:
                    //pdebug(DEBUG_DETAIL, "Setting up Micro8X0 tag.");

                    if (path != null || path.Length != 0)
                    {
                        //pdebug(DEBUG_WARN, "A path is not supported for this PLC type.");
                    }

                    /* if we did not fill in the byte order elsewhere, fill it in now. */
                    if (byte_order == null)
                    {
                        //pdebug(DEBUG_DETAIL, "Using default Micro8x0 byte order.");
                        byte_order = new logix_tag_byte_order();
                    }

                    /* if this was not filled in elsewhere default to generic *Logix */
                    /*HR*               if (tag->vtable == &default_vtable || !tag->vtable)
                                   {
                                       //pdebug(DEBUG_DETAIL, "Setting default Logix vtable.");
                                       tag->vtable = &eip_cip_vtable;
                                   }
                    */
                    use_connected_msg = true;
                    allow_packing = 0;

                    break;

                // case AB_PLC_OMRON_NJNX:
                //     pdebug(DEBUG_DETAIL, "Setting up OMRON NJ/NX Series tag.");

                //     if(str_length(path) == 0) {
                //         pdebug(DEBUG_WARN,"A path is required for this PLC type.");
                //         tag->status = PLCTAG_ERR_BAD_PARAM;
                //         return (plc_tag_p)tag;
                //     }

                //     /* if we did not fill in the byte order elsewhere, fill it in now. */
                //     if(!tag->byte_order) {
                //         pdebug(DEBUG_DETAIL, "Using default Omron byte order.");
                //         tag->byte_order = &omron_njnx_tag_byte_order;
                //     }

                //     /* if this was not filled in elsewhere default to generic *Logix */
                //     if(tag->vtable == &default_vtable || !tag->vtable) {
                //         pdebug(DEBUG_DETAIL, "Setting default Logix vtable.");
                //         tag->vtable = &eip_cip_vtable;
                //     }

                //     tag->use_connected_msg = 1;
                //     tag->allow_packing = attr_get_int(attribs, "allow_packing", 0);

                //     break;

                default:
                    //pdebug(DEBUG_WARN, "Unknown PLC type!");
                    status_ = PLCTAG_ERR_BAD_CONFIG;
                    throw new Exception("PLC desconocido");
                    //return tag;
            }

            /* pass the connection requirement since it may be overridden above. */
            //Attr.attr_set_int(attribs, "use_connected_msg", use_connected_msg ? 1 : 0);

            /* get the element count, default to 1 if missing. */
            elem_count = elemCount; // Attr.attr_get_int(attribs, "elem_count", 1);

            switch (plc_type)
            {
                // case AB_PLC_OMRON_NJNX:
                case PlcType.AB_PLC_LGX:
                /* fall through */
                case PlcType.AB_PLC_MICRO800:
                    /* fill this in when we read the tag. */
                    //tag->elem_size = 0;
                    size = 0;
                    data = null;
                    break;

                default:
                    /* we still need size on non Logix-class PLCs */
                    /* get the element size if it is not already set. */
                    if (elem_size == 0)
                    {
                        elem_size = elemSize ; //Attr.attr_get_int(attribs, "elem_size", 0);
                    }

                    /* Determine the tag size */
                    size = (elem_count) * (elem_size);
                    if (size == 0)
                    {
                        /* failure! Need data_size! */
                        //pdebug(DEBUG_WARN, "Tag size is zero!");
                        status_ = PlcTag.PLCTAG_ERR_BAD_PARAM;
                        throw new Exception("El tamaño del tag es cero");
                        //return tag;
                    }

                    /* this may be changed in the future if this is a tag list request. */
                    //   tag->data = (uint8_t*)mem_alloc(tag->size);
                    data = new byte[size];

                    /*if (data == null)
                    {
                        //pdebug(DEBUG_WARN, "Unable to allocate tag data!");
                        status = PlcTag.PLCTAG_ERR_NO_MEM;
                        throw new Exception("");
                        //return tag;
                    }*/
                    break;
            }

            /*
             * check the tag name, this is protocol specific.
             */

            if (special_tag == 0 && check_tag_name(/*tag,*/ name /*Attr.attr_get_str(attribs, "name", null)*/) != PlcTag.PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_INFO, "Bad tag name!");
                status_ = PLCTAG_ERR_BAD_PARAM;
                throw new Exception("Nombre de tag erroneo");


                //return tag;
            }

            /* kick off a read to get the tag type and size. */
            if (special_tag == 0 /*&& tag->vtable->read*/)
            {
                /* trigger the first read. */
                //pdebug(DEBUG_DETAIL, "Kicking off initial read.");

                first_read = 1;
                read_in_flight = true;
                /*.path->vtable->*/
                read(/*(plc_tag_p)tag*/);
                //       Thread.Sleep(1000);
                //*HR*?                 tag_raise_event((plc_tag_p)tag, PLCTAG_EVENT_READ_STARTED, tag->status);
            }
            else
            {
                //pdebug(DEBUG_DETAIL, "Not kicking off initial read: tag is special or does not have read function.");

                /* force the created event because we do not do an initial read here. */
                //*HR*                    tag_raise_event((plc_tag_p)tag, PLCTAG_EVENT_CREATED, tag->status);
            }

            postCreate(timeout);

            /* get the tag status. */
            //rc = tag.vtable.status(tag);
    /*        rc = status();

            /* check to see if there was an error during tag creation. */
    /*        if (rc != PLCTAG_STATUS_OK && rc != PLCTAG_STATUS_PENDING)
            {
                //pdebug(DEBUG_WARN, "Error %s while trying to create tag!", plc_tag_decode_error(rc));
                /*if (tag.vtable.abort!=null)
                {
                    tag.vtable.abort(tag);
                }*/
    /*            abort();

                /* remove the tag from the hashtable. */
                /*critical_block(tag_lookup_mutex) {
                    hashtable_remove(tags, (int64_t)tag->tag_id);
                }*/
    /*            lock (tags)
                    tags.Remove(tag_id);

                //rc_dec(tag);
                throw new Exception("Error al crear el tag!");
                //return null; // rc;
            }

            //pdebug(DEBUG_DETAIL, "Tag status after creation is %s.", plc_tag_decode_error(rc));

            /*
            * if there is a timeout, then wait until we get
            * an error or we timeout.
            */
    /*        if (timeout > 0 && rc == PLCTAG_STATUS_PENDING)
            {
                Int64 start_time = Alpiste.Utils.Milliseconds.ms(); // time_ms();
                Int64 end_time = start_time + timeout;

                /* wake up the tickler in case it is needed to create the tag. */
    /*            plc_tag_tickler_wake();

                /* we loop as long as we have time left to wait. */
     /*           do
                {
                    Int64 timeout_left = end_time - Alpiste.Utils.Milliseconds.ms(); //time_ms();

                    /* clamp the timeout left to non-negative int range. */
     /*               if (timeout_left < 0)
                    {
                        timeout_left = 0;
                    }

                    if (timeout_left > Int16.MaxValue /* INT_MAX*///)
     /*               {
                        timeout_left = 100; /* MAGIC, only wait 100ms in this weird case. */
     /*               }

                    /* wait for something to happen */
     /*               tag_cond_wait = new Cond();
                    rc = tag_cond_wait.cond_wait(/*tag_cond_wait,*/// (int)timeout_left);

    /*                if (rc != PLCTAG_STATUS_OK)
                    {
                        //pdebug(DEBUG_WARN, "Error %s while waiting for tag creation to complete!", plc_tag_decode_error(rc));
                        /*if (tag->vtable->abort)
                        {
                            tag->vtable->abort(tag);
                        }*/
    /*                    abort();
                         
                        /* remove the tag from the hashtable. */
                        //critical_block(tag_lookup_mutex) {
                        //       hashtable_remove(tags, (int64_t)tag->tag_id);
    /*                    lock (tags)
                            tags.Remove(tag_id);
                        //   }

                        //rc_dec(tag);
                        throw new Exception("Error al crear el tag!");
                        //return null; // rc;
                    }

                    /* get the tag status. */
    /*                rc = status();  // vtable.status(tag);

                    /* check to see if there was an error during tag creation. */
    /*                if (rc != PLCTAG_STATUS_OK && rc != PLCTAG_STATUS_PENDING)
                    {
                        //pdebug(DEBUG_WARN, "Error %s while trying to create tag!", plc_tag_decode_error(rc));
                        /*if (tag.vtable.abort!=null)
                        {
                            tag.vtable.abort(tag);
                        }*/
    /*                    abort();

                        /* remove the tag from the hashtable. */
                        /*critical_block(tag_lookup_mutex) {
                            hashtable_remove(tags, (int64_t)tag->tag_id);
                        }*/
    /*                    lock (tags)
                            tags.Remove(tag_id);

                        //rc_dec(tag);
                        throw new Exception("Timeout expired");
                        //return null; // rc;
                    }
                } while (rc == PLCTAG_STATUS_PENDING && Alpiste.Utils.Milliseconds.ms() /*    time_ms()*/// > end_time);

                /* clear up any remaining flags.  This should be refactored. */
    /*            read_in_flight = false;
                write_in_flight = false;
    */
        }

       

        ~AbTag()
        {
            Console.WriteLine("Destruyendo ABTag");
            Dispose();
        }
        static public AbTag ab_tag_create(attr attribs, callback_func_ex tag_callback_func, Object userdata)
        {
            AbTag tag = null; 
            String path = null;
            int rc = PlcTag.PLCTAG_STATUS_OK;

            /* short circuit for split Omron*/
            if(get_plc_type(attribs) == PlcType.AB_PLC_OMRON_NJNX) {
                //            return omron_tag_create(attribs, tag_callback_func, userdata);
                throw new PLCNotSupportedException();
            }

            /*
             * allocate memory for the new tag.  Do this first so that
             * we have a vehicle for returning status.
            */

            //tag = (ab_tag_p) rc_alloc(sizeof(struct ab_tag_t), (rc_cleanup_func) ab_tag_destroy);
            //if(!tag) {
            //    pdebug(DEBUG_ERROR,"Unable to allocate memory for AB EIP tag!");
            //    return (plc_tag_p) NULL;
            //}

            //pdebug(DEBUG_DETAIL, "tag=%p", tag);

            /*
             * we got far enough to allocate memory, set the default vtable up
             * in case we need to abort later.
             */

            //tag->vtable = &default_vtable;

            /* set up the generic parts. */

            tag = AbTag_select_constructor(attribs, tag_callback_func, userdata);
   /*        rc = tag.plc_tag_generic_init_tag(attribs, tag_callback_func, userdata);
            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_WARN, "Unable to initialize generic tag parts!");
                //rc_dec(tag);
                return null; // (plc_tag_p)NULL;
            }

*/

                //pdebug(DEBUG_DETAIL, "Using vtable %p.", tag->vtable);

                //pdebug(DEBUG_INFO, "Done.");

                return tag;
        }


        public static PlcType get_plc_type(attr attribs)
        {
            String cpu_type = Attr.attr_get_str(attribs, "plc", Attr.attr_get_str(attribs, "cpu", "NONE"));

            if (cpu_type.ToLower() == "plc" || cpu_type.ToLower() == "plc5")
            {
                //pdebug(DEBUG_DETAIL, "Found PLC/5 PLC.");
                return PlcType.AB_PLC_PLC5;
            }
            else if (cpu_type.ToLower() == "slc" || cpu_type.ToLower() == "slc500")
            {
                //pdebug(DEBUG_DETAIL, "Found SLC 500 PLC.");
                return PlcType.AB_PLC_SLC;
            }
            else if (cpu_type.ToLower() == "lgxpccc" || cpu_type.ToLower() == "logixpccc" || 
                    cpu_type.ToLower() == "lgxplc5" || cpu_type.ToLower() == "logixplc5" ||
                    cpu_type.ToLower() == "lgx-pccc" || cpu_type.ToLower() == "logix-pccc" ||
                    cpu_type.ToLower() == "lgx-plc5" || cpu_type.ToLower() == "logix-plc5")
            {
                //pdebug(DEBUG_DETAIL, "Found Logix-class PLC using PCCC protocol.");
                return PlcType.AB_PLC_LGX_PCCC;
            }
            else if (cpu_type.ToLower() == "micrologix800" || cpu_type.ToLower() == "mlgx800" ||
                  cpu_type.ToLower() == "micro800")
            {
                //pdebug(DEBUG_DETAIL, "Found Micro8xx PLC.");
                return PlcType.AB_PLC_MICRO800;
            }
            else if (cpu_type.ToLower() == "micrologix" || cpu_type.ToLower() == "mlgx")
            {
                //pdebug(DEBUG_DETAIL, "Found MicroLogix PLC.");
                return PlcType.AB_PLC_MLGX;
            }
            else if (cpu_type.ToLower() == "compactlogix" || cpu_type.ToLower() == "clgx" || 
                    cpu_type.ToLower() == "lgx" || cpu_type.ToLower() == "controllogix" || 
                    cpu_type.ToLower() == "contrologix" || cpu_type.ToLower() == "logix")
            {
                //pdebug(DEBUG_DETAIL, "Found ControlLogix/CompactLogix PLC.");
                return PlcType.AB_PLC_LGX;
            }
            else if (cpu_type.ToLower() == "omron-njnx" || cpu_type.ToLower() == "omron-nj" || 
                cpu_type.ToLower() == "omron-nx" || cpu_type.ToLower() == "njnx"
                       || cpu_type.ToLower() == "nx1p2")
            {
                //pdebug(DEBUG_DETAIL, "Found OMRON NJ/NX Series PLC.");
                return PlcType.AB_PLC_OMRON_NJNX;
            }
            else
            {
                //pdebug(DEBUG_WARN, "Unsupported device type: %s", cpu_type);

                return PlcType.AB_PLC_NONE;
            }
        }

        int check_cpu(attr attribs)
        {
            PlcType result = get_plc_type(attribs);

            if (result != PlcType.AB_PLC_NONE)
            {
                plc_type = result;
                return PLCTAG_STATUS_OK;
            }
            else
            {
                plc_type = result;
                return PLCTAG_ERR_BAD_DEVICE;
            }
        }

        /*
         * determine the tag's data type and size.  Or at least guess it.
         */

        public int get_tag_data_type(/*ab_tag_p tag,*/ attr attribs)
        {
            int rc = PLCTAG_STATUS_OK;
            String elem_type = null;
            String tag_name = null;
 //*HR*           pccc_addr_t file_addr = { 0 };

            //pdebug(DEBUG_DETAIL, "Starting.");

            switch (plc_type)
            {
                case PlcType.AB_PLC_PLC5:
                case PlcType.AB_PLC_SLC:
                case PlcType.AB_PLC_LGX_PCCC:
                case PlcType.AB_PLC_MLGX:
 /*HR*                   tag_name = Attr.attr_get_str(attribs, "name", null);

                    rc = parse_pccc_logical_address(tag_name, &file_addr);
                    if (rc != PLCTAG_STATUS_OK)
                    {
                        pdebug(DEBUG_WARN, "Unable to parse data file %s!", tag_name);
                        return rc;
                    }

                    tag->elem_size = file_addr.element_size_bytes;
                    tag->file_type = (int)file_addr.file_type;

                    break;
*/
                case PlcType.AB_PLC_LGX:
                case PlcType.AB_PLC_MICRO800:
                    // case AB_PLC_OMRON_NJNX:
                    /* look for the elem_type attribute. */
                    elem_type = Attr.attr_get_str(attribs, "elem_type", null);
                    if (elem_type!=null)
                    {
                        if (elem_type.ToLower() == "lint" || elem_type.ToLower() == "ulint")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of 64-bit integer.");
                            this.elem_size = 8;
                            this.elem_type = ElemType.AB_TYPE_INT64;
                        }
                        else if (elem_type.ToLower() == "dint" || elem_type.ToLower() == "udint")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of 32-bit integer.");
                            elem_size = 4;
                            this.elem_type = ElemType.AB_TYPE_INT32;
                        }
                        else if (elem_type.ToLower() == "int" || elem_type.ToLower() == "uint")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of 16-bit integer.");
                            elem_size = 2;
                            this.elem_type = ElemType.AB_TYPE_INT16;
                        }
                        else if (elem_type.ToLower() == "sint"|| elem_type.ToLower() == "usint")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of 8-bit integer.");
                            elem_size = 1;
                            this.elem_type = ElemType.AB_TYPE_INT8;
                        }
                        else if (elem_type.ToLower() == "bool")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of bit.");
                            elem_size = 1;
                            this.elem_type = ElemType.AB_TYPE_BOOL;
                        }
                        else if (elem_type.ToLower() == "bool array")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of bool array.");
                            elem_size = 4;
                            this.elem_type = ElemType.AB_TYPE_BOOL_ARRAY;
                        }
                        else if (elem_type.ToLower() == "real")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of 32-bit float.");
                            elem_size = 4;
                            this.elem_type = ElemType.AB_TYPE_FLOAT32;
                        }
                        else if (elem_type.ToLower() == "lreal")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of 64-bit float.");
                            elem_size = 8;
                            this.elem_type = ElemType.AB_TYPE_FLOAT64;
                        }
                        else if (elem_type.ToLower() == "string")
                        {
                            //pdebug(DEBUG_DETAIL, "Fount tag element type of string.");
                            this.elem_size = 88;
                            this.elem_type = ElemType.AB_TYPE_STRING;
                        }
                        else if (elem_type.ToLower() == "short string")
                        {
                            //pdebug(DEBUG_DETAIL, "Found tag element type of short string.");
                            this.elem_size = 256; /* TODO - find the real length */
                            this.elem_type = ElemType.AB_TYPE_SHORT_STRING;
                        }
                        else
                        {
                            //pdebug(DEBUG_DETAIL, "Unknown tag type %s", elem_type);
                            return PlcTag.PLCTAG_ERR_UNSUPPORTED;
                        }
                    }
                    else
                    {
                        /*
                         * We have two cases
                         *      * tag listing, but only for CIP PLCs (but not for UDTs!).
                         *      * no type, just elem_size.
                         * Otherwise this is an error.
                         */
                        int elem_size = Attr.attr_get_int(attribs, "elem_size", 0);
                        int cip_plc = !!(this.plc_type == PlcType.AB_PLC_LGX || this.plc_type == PlcType.AB_PLC_MICRO800 /*|| tag->plc_type == AB_PLC_OMRON_NJNX*/) ? 1:0;

                        if (cip_plc!=0)
                        {
                            String tmp_tag_name = Attr.attr_get_str(attribs, "name", null);
                            int special_tag_rc = PlcTag.PLCTAG_STATUS_OK;

                            /* check for special tags. */
     /*HR*                       if (tmp_tag_name.ToLower() == "@raw")
                            {
                                special_tag_rc = setup_raw_tag(tag);
                            }
                            else if (tmp_tag_name.ToLower()!= "@tags")
                            {
                                special_tag_rc = setup_tag_listing_tag(tag, tmp_tag_name);
                            }
                            else if (Str.str_str_cmp_i(tmp_tag_name, "@udt/")!=null)
                            {
                                special_tag_rc = setup_udt_tag(tag, tmp_tag_name);
                            } /* else not a special tag. */

    /*HR*                        if (special_tag_rc != PLCTAG_STATUS_OK)
                            {
                                //pdebug(DEBUG_WARN, "Error parsing tag listing name!");
                                return special_tag_rc;
                            }
    */                    }

                        /* if we did not set an element size yet, set one. */
                        if (this.elem_size == 0)
                        {
                            if (elem_size > 0)
                            {
                                //pdebug(DEBUG_INFO, "Setting element size to %d.", elem_size);
                                this.elem_size = elem_size;
                            }
                        }
                        else
                        {
                            if (elem_size > 0)
                            {
                                //pdebug(DEBUG_WARN, "Tag has elem_size and either is a tag listing or has elem_type, only use one!");
                            }
                        }
                    }

                    break;

                default:
                    //pdebug(DEBUG_WARN, "Unknown PLC type!");
                    return PLCTAG_ERR_BAD_DEVICE;
                    break;
            }

            //pdebug(DEBUG_DETAIL, "Done.");

            return PLCTAG_STATUS_OK;
        }

        public int check_tag_name(/*ab_tag_p tag,*/ String name)
        {
            int rc = PLCTAG_STATUS_OK;
  //*HR*          pccc_addr_t pccc_address;

            if (name == null) {
                //pdebug(DEBUG_WARN,"No tag name parameter found!");
                return PLCTAG_ERR_BAD_PARAM;
            }

  //*HR*          mem_set(&pccc_address, 0, sizeof(pccc_address));

            /* attempt to parse the tag name */
            switch (plc_type) {
                case PlcType.AB_PLC_PLC5:
                //case PlcType.AB_PLC_PLC5:
                case PlcType.AB_PLC_LGX_PCCC:
   /*HR*                 if((rc = parse_pccc_logical_address(name, &pccc_address))) {
                        //pdebug(DEBUG_WARN, "Parse of PCCC-style tag name %s failed!", name);
                        return rc;
                    }
   
                    if (pccc_address.is_bit)
                    {
                        tag->is_bit = 1;
                        tag->bit = (int)(unsigned int)pccc_address.bit;
                        pdebug(DEBUG_DETAIL, "PLC/5 address references bit %d.", tag->bit);
                    }

                    if ((rc = plc5_encode_address(tag->encoded_name, &(tag->encoded_name_size), MAX_TAG_NAME, &pccc_address)) != PLCTAG_STATUS_OK)
                    {
                        pdebug(DEBUG_WARN, "Encoding of PLC/5-style tag name %s failed!", name);
                        return rc;
                    }
    */
                    break;

                case PlcType.AB_PLC_SLC:
                case PlcType.AB_PLC_MLGX:
    /*HR*                if ((rc = parse_pccc_logical_address(name, &pccc_address)))
                    {
                        pdebug(DEBUG_WARN, "Parse of PCCC-style tag name %s failed!", name);
                        return rc;
                    }

                    if (pccc_address.is_bit)
                    {
                        tag->is_bit = 1;
                        tag->bit = (int)(unsigned int)pccc_address.bit;
                        pdebug(DEBUG_DETAIL, "SLC/Micrologix address references bit %d.", tag->bit);
                    }

                    if ((rc = slc_encode_address(tag->encoded_name, &(tag->encoded_name_size), MAX_TAG_NAME, &pccc_address)) != PLCTAG_STATUS_OK)
                    {
                        pdebug(DEBUG_WARN, "Encoding of SLC-style tag name %s failed!", name);
                        return rc;
                    }
        */
                    break;

            case PlcType.AB_PLC_MICRO800:
            case PlcType.AB_PLC_LGX:
                // case AB_PLC_OMRON_NJNX:
                if ((rc = CIP.cip_encode_tag_name(this, name)) != PLCTAG_STATUS_OK)
                {
                    //pdebug(DEBUG_WARN, "parse of CIP-style tag name %s failed!", name);

                    return rc;
                }

                break;

            default:
                /* how would we get here? */
                //pdebug(DEBUG_WARN, "unsupported PLC type %d", tag->plc_type);

                return PlcTag.PLCTAG_ERR_BAD_PARAM;

                break;
            }

        return PlcTag.PLCTAG_STATUS_OK;

        }


        
        



        protected int build_read_request_connected(int byte_offset)
        {
            eip_cip_co_req cip = null;
            int /*byte[]*/ data = 0; // null;
            Request /*ab_request_p*/ req = null;
            int rc = PLCTAG_STATUS_OK;
            byte read_cmd = Defs.AB_EIP_CMD_CIP_READ_FRAG;

            //pdebug(DEBUG_INFO, "Starting.");
            //Session session;
            //this.session.TryGetTarget(out session);
            /* get a request buffer */
            rc = session.session_create_request(tag_id, ref req);
            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_ERROR, "Unable to get new request.  rc=%d", rc);
                return rc;
            }

            /* point the request struct at the buffer */
            //cip = (eip_cip_co_req)(req.data);
            cip = new eip_cip_co_req();

            /* point to the end of the struct */
            //data = (req->data) + sizeof(eip_cip_co_req);
            
            data = eip_cip_co_req.BASE_SIZE;

            /*
             * set up the embedded CIP read packet
             * The format is:
             *
             * uint8_t cmd
             * LLA formatted name
             * uint16_t # of elements to read
             */

            //embed_start = data;
            int embed_start = data;

            /* set up the CIP Read request */
            // if(tag->plc_type == AB_PLC_OMRON_NJNX) {
            //     read_cmd = AB_EIP_CMD_CIP_READ;
            // } else {
            read_cmd = Defs.AB_EIP_CMD_CIP_READ_FRAG;
            // }

            req.data[data] = read_cmd;
            data++;

            /* copy the tag name into the request */
            //mem_copy(data, tag->encoded_name, tag->encoded_name_size);
            Array.Copy(encoded_name, 0, req.data, data, encoded_name_size);
            /*for (int i = 0; i<encoded_name_size; i++)
            {
                req.data[data + i] = encoded_name[i];
                data++;
            }*/

            data += encoded_name_size;

            /* add the count of elements to read. */
            req.data[data] = (byte) (elem_count & 255);
            req.data[data + 1] = (byte) (elem_count >> 8);
            data += 2;

            //*((uint16_le*)data) = h2le16((uint16_t)(tag->elem_count));
            //data += sizeof(uint16_le);

            if (read_cmd == Defs.AB_EIP_CMD_CIP_READ_FRAG)
            {
                /* add the byte offset for this request */
                //*((uint32_le*)data) = h2le32((uint32_t)byte_offset);
                req.data[data] = (byte) (byte_offset & 255);
                req.data[data+1]= (byte) ((byte_offset >> 8) & 255);
                req.data[data + 2] = (byte)((byte_offset >> 16) & 255);
                req.data[data + 3] = (byte)((byte_offset >> 24) & 255);
                //data += sizeof(uint32_le);
                data += 4;
            }

            /* now we go back and fill in the fields of the static part */

            /* encap fields */
            cip.encap_command = /*h2le16(*/Defs.AB_EIP_CONNECTED_SEND; //); /* ALWAYS 0x0070 Unconnected Send*/

            /* router timeout */
            cip.router_timeout = 1; // h2le16(1); /* one second timeout, enough? */

            /* Common Packet Format fields for unconnected send. */
            cip.cpf_item_count = /*h2le16*/(2);                 /* ALWAYS 2 */
            cip.cpf_cai_item_type = /*h2le16(*/Defs.AB_EIP_ITEM_CAI;//);/* ALWAYS 0x00A1 connected address item */
            cip.cpf_cai_item_length = /*h2le16*/(4);            /* ALWAYS 4, size of connection ID*/
            cip.cpf_cdi_item_type = /*h2le16*/(Defs.AB_EIP_ITEM_CDI);/* ALWAYS 0x00B1 - connected Data Item */
            cip.cpf_cdi_item_length = /*h2le16*/((UInt16)(data - embed_start+ 2/*HR*???*/));  //  (byte)(cip.cpf_conn_seq_num))) /* REQ: fill in with length of remaining data. */

            byte[] cip_data = cip.encodedData();

            Array.Copy(cip.encodedData(), 0, req.data, 0, eip_cip_co_req.BASE_SIZE);
            /*for (int i = 0; i < cip_data.Length; i++)
            {
                req.data[i] = cip_data[i];
            }*/

            /* set the size of the request */
            req.request_size = (int)(data /*- (req.data)*/);

            /* set the session so that we know what session the request is aiming at */
            //req->session = tag->session;

            req.allow_packing = allow_packing;



            /* add the request to the session's list. */
            
            rc = session.session_add_request(req);
    

            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_ERROR, "Unable to add request to session! rc=%d", rc);
                //tag.req = rc_dec(req);
                return rc;
            }

            /* save the request for later */
            this.req = req;

            //pdebug(DEBUG_INFO, "Done");

            return PLCTAG_STATUS_OK;
        }

        /*
         * ab_tag_status
         *
         * Generic status checker.   May be overridden by individual PLC types.
         */
        protected int ab_tag_status()
        {
            int rc = PLCTAG_STATUS_OK;

            if (read_in_progress != 0)
            {
                return PLCTAG_STATUS_PENDING;
            }

            if (write_in_progress != 0)
            {
                return PLCTAG_STATUS_PENDING;
            }

            if (session != null)
            {
                rc = status_;
            }
            else
            {
                /* this is not OK.  This is fatal! */
                rc = PLCTAG_ERR_CREATE;
            }

            return rc;
        }

        protected int tag_tickler(/*ab_tag_p tag*/)
        {
            int rc = PLCTAG_STATUS_OK;

            //pdebug(DEBUG_SPEW, "Starting.");

            if (read_in_progress!=0)
            {
                if (use_connected_msg)
                {
                    rc = check_read_status_connected(/*tag*/);
                }
                else
                {
                    rc = check_read_status_unconnected(/*tag*/);
                }

                status_ = rc;

                /* if the operation completed, make a note so that the callback will be called. */
                if (read_in_progress==0)
                {
                    /* done! */
                    if (first_read!=0)
                    {
                        first_read = 0;
  //*HR*                      tag_raise_event(/*(plc_tag_p)tag,*/ PLCTAG_EVENT_CREATED, (int8_t)rc);
                    }

                    read_complete = true;
                }

                //pdebug(DEBUG_SPEW, "Done.  Read in progress.");

                return rc;
            }

            if (write_in_progress!=0)
            {
                if (use_connected_msg)
                {
  //*HR*                  rc = check_write_status_connected(/*tag*/);
                }
                else
                {
  //*HR*                  rc = check_write_status_unconnected(/*tag*/);
                }

                status_ = rc;

                /* if the operation completed, make a note so that the callback will be called. */
                if (write_in_progress==0)
                {
                    write_complete = true;
                }

                //pdebug(DEBUG_SPEW, "Done. Write in progress.");

                return rc;
            }

            //pdebug(DEBUG_SPEW, "Done.  No operation in progress.");

            return status_;
        }


        /*
 * check_read_status_connected
 *
 * This routine checks for any outstanding requests and copies in data
 * that has arrived.  At the end of the request, it will clean up the request
 * buffers.  This is not thread-safe!  It should be called with the tag mutex
 * locked!
 */

        int check_read_status_connected(/*ab_tag_p tag*/)
        {
            int rc = PLCTAG_STATUS_OK;
            eip_cip_co_resp cip_resp;
            byte[] data;
            int /*uint8_t**/ data_end;
            bool partial_data = false;
            Request /*ab_request_p*/ request = null;

            //pdebug(DEBUG_SPEW, "Starting.");

            /*if (!tag)
            {
                pdebug(DEBUG_ERROR, "Null tag pointer passed!");
                return PLCTAG_ERR_NULL_PTR;
            }

            /* guard against the request being deleted out from underneath us. */
            //request = rc_inc(tag->req);
            request = req;
            rc = check_read_request_status(request);
            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_DETAIL, "Read request status is not OK.");
                //rc_dec(request);
                return rc;
            }

            /* the request reference is still valid. */

            /* point to the data */
            //cip_resp = (eip_cip_co_resp*)(request->data);
            cip_resp = eip_cip_co_resp.createFromData(request.data);

            /* point to the start of the data */
            //data = (request->data) + sizeof(eip_cip_co_resp);
            data = new byte[request.data.Length - eip_cip_co_resp.base_size];
            Array.Copy(request.data, eip_cip_co_resp.base_size, data, 0, request.data.Length - eip_cip_co_resp.base_size);
            int data_ = eip_cip_co_resp.base_size;

            /* point the end of the data */
            //data_end = (request->data + le2h16(cip_resp->encap_length) + sizeof(eip_encap));
            data_end = cip_resp.encap_length + eip_encap.encap_size;

            /* check the status */
            do
            {
                // ptrdiff_t payload_size = (data_end - data);
                int payload_size = data_end;

                if (cip_resp.encap_command != Defs.AB_EIP_CONNECTED_SEND)
                {
                    //pdebug(DEBUG_WARN, "Unexpected EIP packet type received: %d!", cip_resp->encap_command);
                    rc = PLCTAG_ERR_BAD_DATA;
                    break;
                }

                if (cip_resp.encap_status != Defs.AB_EIP_OK)
                {
                    //pdebug(DEBUG_WARN, "EIP command failed, response code: %d", le2h32(cip_resp->encap_status));
                    rc = PLCTAG_ERR_REMOTE_ERR;
                    break;
                }

                /*
                 * FIXME
                 *
                 * It probably should not be necessary to check for both as setting the type to anything other
                 * than fragmented is error-prone.
                 */

                if (cip_resp.reply_service != (Defs.AB_EIP_CMD_CIP_READ_FRAG | Defs.AB_EIP_CMD_CIP_OK)
                               && cip_resp.reply_service != (Defs.AB_EIP_CMD_CIP_READ | Defs.AB_EIP_CMD_CIP_OK))
                {
                    //pdebug(DEBUG_WARN, "CIP response reply service unexpected: %d", cip_resp->reply_service);
                    rc = PLCTAG_ERR_BAD_DATA;
                    break;
                }

                if (cip_resp.status != Defs.AB_CIP_STATUS_OK && cip_resp.status != Defs.AB_CIP_STATUS_FRAG)
                {
                    //pdebug(DEBUG_WARN, "CIP read failed with status: 0x%x %s", cip_resp->status, decode_cip_error_short((uint8_t*)&cip_resp->status));
                    //pdebug(DEBUG_INFO, decode_cip_error_long((uint8_t*)&cip_resp->status));

 //*HR*                   rc = decode_cip_error_code(cip_resp.status);

                    break;
                }

                /* check to see if this is a partial response. */
                partial_data = (cip_resp.status == Defs.AB_CIP_STATUS_FRAG);

                /*
                 * check to see if there is any data to process.  If this is a packed
                 * response, there might not be.
                  */
                payload_size = data_end;
                
                if (payload_size > 0)
                {
                    /* skip the copy if we already have type data */
                    if (encoded_type_info_size == 0)
                    {
                        int type_length = 0;

                        /* the first byte of the response is a type byte. */
                        //pdebug(DEBUG_DETAIL, "type byte = %d (0x%02x)", (int)*data, (int)*data);

                        if (cip_lookup_encoded_type_size(data[0], ref type_length) == PLCTAG_STATUS_OK)
                        {
                            /* found it and we got the type data size */

                            /* some types use the second byte to indicate how many bytes more are used. */
                            if (type_length == 0)
                            {
             //*HR*                   type_length = *(data + 1) + 2;
                            }

                            if (type_length <= 0)
                            {
                                //pdebug(DEBUG_WARN, "Unable to determine type data length for type byte 0x%02x!", *data);
                                rc = PLCTAG_ERR_UNSUPPORTED;
                                break;
                            }

                            //pdebug(DEBUG_DETAIL, "Type data is %d bytes long.", type_length);
                            //pdebug_dump_bytes(DEBUG_DETAIL, data, type_length);

                            encoded_type_info_size = type_length;
                            //mem_copy(tag->encoded_type_info, data, tag->encoded_type_info_size);
                            encoded_type_info = new byte[encoded_type_info_size];
                            Array.Copy(data, 0, encoded_type_info, 0, encoded_type_info_size);
                        }
                        else
                        {
                            //pdebug(DEBUG_WARN, "Unsupported data type returned, type byte=0x%02x", *data);
                            rc = PLCTAG_ERR_UNSUPPORTED;
                            break;
                        }
                    }

                    /* skip past the type data */
                    // data += (tag->encoded_type_info_size);
                    data_ += encoded_type_info_size;
                    int offset_ = encoded_type_info_size;

                    /* check payload size now that we have bumped past the data type info. */
                    payload_size = (data_end - data_);

                    /* copy the data into the tag and realloc if we need more space. */
                    if (payload_size + offset > size)
                    {
                         size = (int)payload_size + offset;
                         elem_size = size / elem_count;

                         //pdebug(DEBUG_DETAIL, "Increasing tag buffer size to %d bytes.", tag->size);

                         //data = (uint8_t*)mem_realloc(tag->data, tag->size);
                         this.data = new byte[size];
                         
                         /*if (!tag->data)
                           {
                               pdebug(DEBUG_WARN, "Unable to reallocate tag data memory!");
                               rc = PLCTAG_ERR_NO_MEM;
                               break;
                           } */
                    }

                     //pdebug(DEBUG_INFO, "Got %d bytes of data", (int)payload_size);

                    /*
                     * copy the data, but only if this is not
                     * a pre-read for a subsequent write!  We do not
                     * want to overwrite the data the upstream has
                     * put into the tag's data buffer.
                     */
                     if (pre_write_read==0)
                     {
                        // mem_copy(tag->data + tag->offset, data, (int)(payload_size));
                        Array.Copy(data, offset_, this.data, offset, payload_size);
                     }

                      /* bump the byte offset */
                      offset += (int)(payload_size);
                }
                else
                {
                    //pdebug(DEBUG_DETAIL, "Response returned no data and no error.");
                }

                /* set the return code */
                rc = PLCTAG_STATUS_OK;
             } while (false);

             /* clean up the request */
             request.abort_request = 1;
             //           tag->req = rc_dec(request);

             /*
              * huh?  Yes, we do it a second time because we already had
              * a reference and got another at the top of this function.
              * So we need to remove it twice.   Once for the capture above,
              * and once for the original reference.
              */

            /*HR*            rc_dec(request);

             /* are we actually done? */
             if (rc == PLCTAG_STATUS_OK)
             {
                 /* this particular read is done. */
                 read_in_progress = 0;

                 /* skip if we are doing a pre-write read. */
                 if ((pre_write_read==0) && partial_data)
                 {
                     /* call read start again to get the next piece */
                     //pdebug(DEBUG_DETAIL, "calling tag_read_start() to get the next chunk.");
  //*HR*                   rc = tag_read_start(tag);
                 }
                 else
                 {
                      offset = 0;

                      /* if this is a pre-read for a write, then pass off to the write routine */
                      if (pre_write_read!=0)
                      {
                            //debug(DEBUG_DETAIL, "Restarting write call now.");
                            pre_write_read = 0;
    //HR                        rc = tag_write_start(tag);
                      }
                 }
             }

                 /* this is not an else clause because the above if could result in bad rc. */
             if (rc != PLCTAG_STATUS_OK && rc != PLCTAG_STATUS_PENDING)
             {
                 /* error ! */
                //pdebug(DEBUG_WARN, "Error received!");

                /* clean up everything. */
 //*HR*               ab_tag_abort();
             }
            
            //pdebug(DEBUG_SPEW, "Done.");

            return rc;
        }



        static int check_read_status_unconnected(/*ab_tag_p tag*/)
        {
            int rc = PLCTAG_STATUS_OK;
/*HR*            eip_cip_uc_resp* cip_resp;
            uint8_t* data;
            uint8_t* data_end;
            int partial_data = 0;
            ab_request_p request = NULL;

            pdebug(DEBUG_SPEW, "Starting.");

            if (!tag)
            {
                pdebug(DEBUG_ERROR, "Null tag pointer passed!");
                return PLCTAG_ERR_NULL_PTR;
            }

            /* guard against the request being deleted out from underneath us. */
/*HR*            request = rc_inc(tag->req);
            rc = check_read_request_status(tag, request);
            if (rc != PLCTAG_STATUS_OK)
            {
                pdebug(DEBUG_DETAIL, "Read request status is not OK.");
                rc_dec(request);
                return rc;
            }

            /* the request reference is still valid. */

            /* point to the data */
/*HR*            cip_resp = (eip_cip_uc_resp*)(request->data);

            /* point to the start of the data */
/*HR*            data = (request->data) + sizeof(eip_cip_uc_resp);

            /* point the end of the data */
/*HR*            data_end = (request->data + le2h16(cip_resp->encap_length) + sizeof(eip_encap));

            /* check the status */
/*HR*            do
            {
                ptrdiff_t payload_size = (data_end - data);

                if (le2h16(cip_resp->encap_command) != AB_EIP_UNCONNECTED_SEND)
                {
                    pdebug(DEBUG_WARN, "Unexpected EIP packet type received: %d!", cip_resp->encap_command);
                    rc = PLCTAG_ERR_BAD_DATA;
                    break;
                }

                if (le2h32(cip_resp->encap_status) != AB_EIP_OK)
                {
                    pdebug(DEBUG_WARN, "EIP command failed, response code: %d", le2h32(cip_resp->encap_status));
                    rc = PLCTAG_ERR_REMOTE_ERR;
                    break;
                }

                /*
                 * TODO
                 *
                 * It probably should not be necessary to check for both as setting the type to anything other
                 * than fragmented is error-prone.
                 */

/*HR*                if (cip_resp->reply_service != (AB_EIP_CMD_CIP_READ_FRAG | AB_EIP_CMD_CIP_OK)
                    && cip_resp->reply_service != (AB_EIP_CMD_CIP_READ | AB_EIP_CMD_CIP_OK))
                {
                    pdebug(DEBUG_WARN, "CIP response reply service unexpected: %d", cip_resp->reply_service);
                    rc = PLCTAG_ERR_BAD_DATA;
                    break;
                }

                if (cip_resp->status != AB_CIP_STATUS_OK && cip_resp->status != AB_CIP_STATUS_FRAG)
                {
                    pdebug(DEBUG_WARN, "CIP read failed with status: 0x%x %s", cip_resp->status, decode_cip_error_short((uint8_t*)&cip_resp->status));
                    pdebug(DEBUG_INFO, decode_cip_error_long((uint8_t*)&cip_resp->status));

                    rc = decode_cip_error_code((uint8_t*)&cip_resp->status);

                    break;
                }

                /* check to see if this is a partial response. */
/*HR*                partial_data = (cip_resp->status == AB_CIP_STATUS_FRAG);

                /*
                * check to see if there is any data to process.  If this is a packed
                * response, there might not be.
                */
/*HR*                payload_size = (data_end - data);
                if (payload_size > 0)
                {
                    /* skip the copy if we already have type data */
/*HR*                    if (tag->encoded_type_info_size == 0)
                    {
                        int type_length = 0;

                        /* the first byte of the response is a type byte. */
/*HR*                        pdebug(DEBUG_DETAIL, "type byte = %d (0x%02x)", (int)*data, (int)*data);

                        if (cip_lookup_encoded_type_size(*data, &type_length) == PLCTAG_STATUS_OK)
                        {
                            /* found it and we got the type data size */

                            /* some types use the second byte to indicate how many bytes more are used. */
/*HR*                            if (type_length == 0)
                            {
                                type_length = *(data + 1) + 2;
                            }

                            if (type_length <= 0)
                            {
                                pdebug(DEBUG_WARN, "Unable to determine type data length for type byte 0x%02x!", *data);
                                rc = PLCTAG_ERR_UNSUPPORTED;
                                break;
                            }

                            pdebug(DEBUG_DETAIL, "Type data is %d bytes long.", type_length);
                            pdebug_dump_bytes(DEBUG_DETAIL, data, type_length);

                            tag->encoded_type_info_size = type_length;
                            mem_copy(tag->encoded_type_info, data, tag->encoded_type_info_size);
                        }
                        else
                        {
                            pdebug(DEBUG_WARN, "Unsupported data type returned, type byte=0x%02x", *data);
                            rc = PLCTAG_ERR_UNSUPPORTED;
                            break;
                        }
                    }

                    /* skip past the type data */
/*HR*                    data += (tag->encoded_type_info_size);

                    /* check payload size now that we have bumped past the data type info. */
/*HR*                    payload_size = (data_end - data);

                    /* copy the data into the tag and realloc if we need more space. */
/*HR*                    if (payload_size + tag->offset > tag->size)
                    {
                        tag->size = (int)payload_size + tag->offset;
                        tag->elem_size = tag->size / tag->elem_count;

                        pdebug(DEBUG_DETAIL, "Increasing tag buffer size to %d bytes.", tag->size);

                        tag->data = (uint8_t*)mem_realloc(tag->data, tag->size);
                        if (!tag->data)
                        {
                            pdebug(DEBUG_WARN, "Unable to reallocate tag data memory!");
                            rc = PLCTAG_ERR_NO_MEM;
                            break;
                        }
                    }

                    pdebug(DEBUG_INFO, "Got %d bytes of data", (int)payload_size);

                    /*
                    * copy the data, but only if this is not
                    * a pre-read for a subsequent write!  We do not
                    * want to overwrite the data the upstream has
                    * put into the tag's data buffer.
                    */
/*HR*                    if (!tag->pre_write_read)
                    {
                        mem_copy(tag->data + tag->offset, data, (int)payload_size);
                    }

                    /* bump the byte offset */
/*HR*                    tag->offset += (int)payload_size;
                }
                else
                {
                    pdebug(DEBUG_DETAIL, "Response returned no data and no error.");
                }

                /* set the return code */
/*HR*                rc = PLCTAG_STATUS_OK;
            } while (0);


            /* clean up the request */
/*HR*            request->abort_request = 1;
            tag->req = rc_dec(request);

            /*
             * huh?  Yes, we do it a second time because we already had
             * a reference and got another at the top of this function.
             * So we need to remove it twice.   Once for the capture above,
             * and once for the original reference.
             */

/*HR*            rc_dec(request);

            /* are we actually done? */
/*HR*            if (rc == PLCTAG_STATUS_OK)
            {
                /* this read is done. */
/*HR*                tag->read_in_progress = 0;

                /* skip if we are doing a pre-write read. */
/*HR*                if (!tag->pre_write_read && partial_data)
                {
                    /* call read start again to get the next piece */
/*HR*                    pdebug(DEBUG_DETAIL, "calling tag_read_start() to get the next chunk.");
                    rc = tag_read_start(tag);
                }
                else
                {
                    tag->offset = 0;

                    /* if this is a pre-read for a write, then pass off to the write routine */
/*HR*                    if (tag->pre_write_read)
                    {
                        pdebug(DEBUG_DETAIL, "Restarting write call now.");
                        tag->pre_write_read = 0;
                        rc = tag_write_start(tag);
                    }
                }
            }

            /* this is not an else clause because the above if could result in bad rc. */
/*HR*            if (rc != PLCTAG_STATUS_OK && rc != PLCTAG_STATUS_PENDING)
            {
                /* error ! */
/*HR*                pdebug(DEBUG_WARN, "Error received!");

                /* clean up everything. */
/*HR*                ab_tag_abort(tag);
            }

            /* release the referene to the request. */

            // FIXME - why is this different than the connected case?
            // rc_dec(request);

            //pdebug(DEBUG_SPEW, "Done.");

            return rc;
        }

        int check_read_request_status(/*ab_tag_p tag,*/ Request /*ab_request_p*/ request)
        {
            int rc = PLCTAG_STATUS_OK;

            //pdebug(DEBUG_SPEW, "Starting.");

            if (request == null)
            {
                read_in_progress = 0;
                offset = 0;

                //pdebug(DEBUG_WARN, "Read in progress, but no request in flight!");

                return PLCTAG_ERR_READ;
            }

            /* we now have a valid reference to the request. */

            /* request can be used by more than one thread at once. */

            try
            {
                request._lock = false;
                Request._spinLock.Enter(ref request._lock);

                //spin_block(&request._lock)
                do
                {
                    if (request.resp_received == 0)
                    {
                        rc = PLCTAG_STATUS_PENDING;
                        break;
                    }

                    /* check to see if it was an abort on the session side. */
                    if (request.status != PLCTAG_STATUS_OK)
                    {
                        rc = request.status;
                        request.abort_request = 1;

                        //pdebug(DEBUG_WARN, "Session reported failure of request: %s.", plc_tag_decode_error(rc));

                        read_in_progress = 0;
                        offset = 0;

                        /* TODO - why is this here? */
                        size = elem_count * elem_size;

                        break;
                    }
                } while (false);
            }
            finally
            {
                if (request._lock)
                    Request._spinLock.Exit();
            }

            if (rc != PLCTAG_STATUS_OK)
            {
                if (rc_is_error(rc))
                {
                    /* the request is dead, from session side. */
                    read_in_progress = 0;
                    offset = 0;

                    req = null;
                }

                //pdebug(DEBUG_DETAIL, "Read not ready with status %s.", plc_tag_decode_error(rc));

                return rc;
            }

            //pdebug(DEBUG_SPEW, "Done.");

            return rc;
        }

        public bool rc_is_error(int rc)
        {
            return rc < PLCTAG_STATUS_OK;
        }

        /*
 * ab_tag_destroy
 *
 * This blocks on the global library mutex.  This should
 * be fixed to allow for more parallelism.  For now, safety is
 * the primary concern.
 */

        protected override void Dispose(bool disposing)
        {
            //Session session = null;

            //pdebug(DEBUG_INFO, "Starting.");

            /* already destroyed? */
            /*if (!tag)
            {
                pdebug(DEBUG_WARN, "Tag pointer is null!");

                return;
            }*/

            /* abort anything in flight */
            //ab_tag_abort();
            abort();
            //this.session.TryGetTarget(out session);
            //session = this.session;

            /* tags should always have a session.  Release it. */
            //pdebug(DEBUG_DETAIL, "Getting ready to release tag session %p", tag->session);
            /*if (session)
            {
                pdebug(DEBUG_DETAIL, "Removing tag from session.");
                rc_dec(session);
                tag->session = NULL;
            }
            else
            {
                pdebug(DEBUG_WARN, "No session pointer!");
            }*/

            //this.session = null; ;

            /*if (tag->ext_mutex)
            {
                mutex_destroy(&(tag->ext_mutex));
                tag->ext_mutex = NULL;
            }*/
            //ext_mutex = null;

            /*if (tag->api_mutex)
            {
                mutex_destroy(&(tag->api_mutex));
                tag->api_mutex = NULL;
            }*/
            //api_mutex = null;

            /*if (tag->tag_cond_wait)
            {
                cond_destroy(&(tag->tag_cond_wait));
                tag->tag_cond_wait = NULL;
            }*/
            //tag_cond_wait = null;

            /*if (tag->byte_order && tag->byte_order->is_allocated)
            {
                mem_free(tag->byte_order);
                tag->byte_order = NULL;
            }

            if (tag->data)
            {
                mem_free(tag->data);
                tag->data = NULL;
            }*/

            //pdebug(DEBUG_INFO, "Finished releasing all tag resources.");

            //pdebug(DEBUG_INFO, "done");
            Object o;
            session.tags_references.Remove(this/*.weakTag*/);
            if (session.tags_references.Count ==0 )
            {
                Session.sessions.Remove(session);  
                session.Dispose();
            }
            base.Dispose(disposing);
        }

        protected int tag_read_start()
        {
            int rc = PLCTAG_STATUS_OK;

            //pdebug(DEBUG_INFO, "Starting");

            if (read_in_progress != 0 || write_in_progress != 0)
            {
                //pdebug(DEBUG_WARN, "Read or write operation already in flight!");
                return PLCTAG_ERR_BUSY;
            }

            /* mark the tag read in progress */
            read_in_progress = 1;

            /* i is the index of the first new request */
            if (use_connected_msg)
            {
                // if(tag->tag_list) {
                //     rc = build_tag_list_request_connected(tag);
                // } else {
                rc = build_read_request_connected(offset);
                // }
            }
            else
            {
                //*HR*                rc = build_read_request_unconnected(offset);
            }

            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_WARN, "Unable to build read request!");

                read_in_progress = 0;

                return rc;
            }

            //pdebug(DEBUG_INFO, "Done.");

            return PLCTAG_STATUS_PENDING;

        }

        /*
* tag_write_common_start
*
* This must be called from one thread alone, or while the tag mutex is
* locked.
*
* The routine starts the process of writing to a tag.
*/

        public int tag_write_start(/*ab_tag_p tag*/)
        {
            int rc = PLCTAG_STATUS_OK;

            //pdebug(DEBUG_INFO, "Starting");

            if (read_in_progress != 0 || write_in_progress != 0)
            {
                //pdebug(DEBUG_WARN, "Read or write operation already in flight!");
                return PLCTAG_ERR_BUSY;
            }

            /* the write is now in flight */
            write_in_progress = 1;

            /*
             * if the tag has not been read yet, read it.
             *
             * This gets the type data and sets up the request
             * buffers.
             */

            if (first_read != 0)
            {
                //pdebug(DEBUG_DETAIL, "No read has completed yet, doing pre-read to get type information.");

                pre_write_read = 1;
                write_in_progress = 0; /* temporarily mask this off */

                return tag_read_start();
            }

            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_WARN, "Unable to calculate write sizes!");
                write_in_progress = 0;

                return rc;
            }

            if (use_connected_msg)
            {
                rc = build_write_request_connected(offset);
            }
            else
            {
                //*HR*               rc = build_write_request_unconnected(tag, tag->offset);
            }

            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_WARN, "Unable to build write request!");
                write_in_progress = 0;

                return rc;
            }

            //pdebug(DEBUG_INFO, "Done.");

            return PLCTAG_STATUS_PENDING;
        }

        int build_write_request_connected(/*ab_tag_p tag,*/ int byte_offset)
        {
            int rc = PLCTAG_STATUS_OK;
            eip_cip_co_req cip = null;
            int data = 0;
            Request /*ab_request_p*/ req = null;
            int multiple_requests = 0;
            int write_size = 0;
            int str_pad_to_multiple_bytes = 1;

            //pdebug(DEBUG_INFO, "Starting.");

            if (is_bit)
            {
                //*HR*                return build_write_bit_request_connected(tag);
            }

            /* get a request buffer */
            //Session session;
            //this.session.TryGetTarget(out session);
            rc = session.session_create_request(/*tag->session, */tag_id, ref req);
            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_ERROR, "Unable to get new request.  rc=%d", rc);
                return rc;
            }

            rc = calculate_write_data_per_packet();
            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_ERROR, "Unable to calculate valid write data per packet!.  rc=%s", plc_tag_decode_error(rc));
                return rc;
            }

            if (write_data_per_packet < size)
            {
                multiple_requests = 1;
            }

            // if(multiple_requests && tag->plc_type == AB_PLC_OMRON_NJNX) {
            //     pdebug(DEBUG_WARN, "Tag too large for unfragmented request on Omron PLC!");
            //     return PLCTAG_ERR_TOO_LARGE;
            // }

            //cip = (eip_cip_co_req*)(req->data);
            cip = new eip_cip_co_req();

            /* point to the end of the struct */
            data = eip_cip_co_req.BASE_SIZE;// (req->data) + sizeof(eip_cip_co_req);

            /*
             * set up the embedded CIP read packet
             * The format is:
             *
             * uint8_t cmd
             * LLA formatted name
             * data type to write
             * uint16_t # of elements to write
             * data to write
             */

            /*
             * set up the CIP Read request type.
             * Different if more than one request.
             *
             * This handles a bug where attempting fragmented requests
             * does not appear to work with a single boolean.
             */
            //*data = (multiple_requests) ? AB_EIP_CMD_CIP_WRITE_FRAG : AB_EIP_CMD_CIP_WRITE;
            byte aux = (multiple_requests != 0) ? Defs.AB_EIP_CMD_CIP_WRITE_FRAG : Defs.AB_EIP_CMD_CIP_WRITE;
            req.data[data] = aux;
            data++;

            /* copy the tag name into the request */
            //mem_copy(data, tag->encoded_name, tag->encoded_name_size);
            Array.Copy(encoded_name, 0, req.data, data, encoded_name_size);
            data += encoded_name_size;

            /* copy encoded type info */
            if (encoded_type_info_size != 0)
            {
                //mem_copy(data, tag->encoded_type_info, tag->encoded_type_info_size);
                Array.Copy(encoded_type_info, 0, req.data, data, encoded_type_info_size);
                data += encoded_type_info_size;
            }
            else
            {
                //pdebug(DEBUG_WARN, "Data type unsupported!");
                return PLCTAG_ERR_UNSUPPORTED;
            }

            /* copy the item count, little endian */
            //*((uint16_le*)data) = h2le16((uint16_t)(tag->elem_count));
            req.data[data] = (byte)(elem_count & 255);
            req.data[data + 1] = (byte)(elem_count >> 8);
            data += 2; // sizeof(uint16_le);

            if (multiple_requests != 0)
            {
                /* put in the byte offset */
                //*((uint32_le*)data) = h2le32((uint32_t)(byte_offset));
               req.data[data] = (byte)(byte_offset & 255);
                req.data[data + 1] = (byte)((byte_offset >> 8) & 255);
                req.data[data + 2] = (byte)((byte_offset >> 26) & 255);
                req.data[data + 3] = (byte)((byte_offset >> 24) & 255);

                data += 4; // sizeof(uint32_le);
            }

            /* how much data to write? */
            write_size = size - offset;

            if (write_size > write_data_per_packet)
            {
                write_size = write_data_per_packet;
            }

            /* now copy the data to write */
            //mem_copy(data, tag->data + tag->offset, write_size);
            Array.Copy(this.data, offset, req.data, data, write_size);
            data += write_size;
            offset += write_size;

            /* need to pad data to multiple of either 1, 2 or 4 bytes */
            /* for some PLCs (OmronNJ), padding causes issues when writing counted strings as it creates a mismatch between
                the length of the string and the count integer, therefor this padding can be disabled using the str_pad_16_bits attribute */
            str_pad_to_multiple_bytes = byte_order.str_pad_to_multiple_bytes;
            if ((str_pad_to_multiple_bytes == 2 || str_pad_to_multiple_bytes == 4) && write_size != 0)
            {
                if (write_size % str_pad_to_multiple_bytes != 0)
                {
                    int pad_size = str_pad_to_multiple_bytes - (write_size % str_pad_to_multiple_bytes);
                    for (int i = 0; i < pad_size; i++)
                    {
                        req.data[data] = 0;
                        data++;
                    }
                }
            }

            /* now we go back and fill in the fields of the static part */

            /* encap fields */
            cip.encap_command = /*h2le16(*/Defs.AB_EIP_CONNECTED_SEND;/*); /* ALWAYS 0x0070 Unconnected Send*/

            /* router timeout */
            cip.router_timeout = 1; // h2le16(1); /* one second timeout, enough? */

            /* Common Packet Format fields for unconnected send. */
            cip.cpf_item_count = 2; // h2le16(2);                 /* ALWAYS 2 */
            cip.cpf_cai_item_type = /*h2le16(*/ Defs.AB_EIP_ITEM_CAI;/* ALWAYS 0x00A1 connected address item */
            cip.cpf_cai_item_length = 4; // h2le16(4);            /* ALWAYS 4, size of connection ID*/
            cip.cpf_cdi_item_type = /*h2le16(*/ Defs.AB_EIP_ITEM_CDI;/* ALWAYS 0x00B1 - connected Data Item */
   /*HR*???*/          cip.cpf_cdi_item_length = /*h2le16(*/(UInt16)(data - eip_cip_co_req.BASE_SIZE + 2); //- (uint8_t*)(&cip->cpf_conn_seq_num))); /* REQ: fill in with length of remaining data. */

            /* set the size of the request */
            req.request_size = (int)(data); // - (req->data));

            /* allow packing if the tag allows it. */
            req.allow_packing = allow_packing;

            /* add the request to the session's list. */
            Array.Copy(cip.encodedData(), req.data, eip_cip_co_req.BASE_SIZE);
            rc = session.session_add_request(req);

            //Thread.Sleep(10000000);

            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_ERROR, "Unable to add request to session! rc=%d", rc);
                //req = rc_dec(req);
                return rc;
            }

            /* save the request for later */
            this.req = req;

            //pdebug(DEBUG_INFO, "Done");

            return PLCTAG_STATUS_OK;
        }


        int calculate_write_data_per_packet(/*ab_tag_p tag*/)
        {
            int overhead = 0;
            int data_per_packet = 0;
            int max_payload_size = 0;

            //pdebug(DEBUG_DETAIL, "Starting.");
            //Session session;
            //this.session.TryGetTarget(out session);
            /* if we are here, then we have all the type data etc. */
            if (use_connected_msg)
            {
                //pdebug(DEBUG_DETAIL, "Connected tag.");
                max_payload_size = session.session_get_max_payload();
                overhead = 1                               /* service request, one byte */
                            + encoded_name_size        /* full encoded name */
                            + encoded_type_info_size   /* encoded type size */
                            + 2                             /* element count, 16-bit int */
                            + 4                             /* byte offset, 32-bit int */
                            + 8;                            /* MAGIC fudge factor */
            }
            else
            {
                //pdebug(DEBUG_DETAIL, "Unconnected tag.");
                max_payload_size = session.session_get_max_payload();
                overhead = 1                               /* service request, one byte */
                            + encoded_name_size        /* full encoded name */
                            + encoded_type_info_size   /* encoded type size */
                            + session.conn_path_size + 2       /* encoded device path size plus two bytes for length and padding */
                            + 2                             /* element count, 16-bit int */
                            + 4                             /* byte offset, 32-bit int */
                            + 8;                            /* MAGIC fudge factor */
            }

            data_per_packet = max_payload_size - overhead;

            //pdebug(DEBUG_DETAIL, "Write packet maximum size is %d, write overhead is %d, and write data per packet is %d.", max_payload_size, overhead, data_per_packet);

            if (data_per_packet <= 0)
            {
                //pdebug(DEBUG_WARN,
                //       "Unable to send request.  Packet overhead, %d bytes, is too large for packet, %d bytes!",
                //       overhead,
                //       max_payload_size);
                return PLCTAG_ERR_TOO_LARGE;
            }

            /* we want a multiple of 8 bytes */
            data_per_packet &= 0xFFFFF8;

            write_data_per_packet = data_per_packet;

            //pdebug(DEBUG_DETAIL, "Done.");

            return PLCTAG_STATUS_OK;
        }

    }
}
