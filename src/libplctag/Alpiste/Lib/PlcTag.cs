using System;
using System.Diagnostics;
using System.Threading;
using LibPlcTag_;
using Alpiste.Utils;
using static Alpiste.Lib.PlcTag;
using Alpiste.Protocol.System_;
using Alpiste.Protocol.AB;
//using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using static libplctag.NativeImport.plctag;

namespace Alpiste.Lib
{
    

    public enum ElemType {
        AB_TYPE_BOOL,
        AB_TYPE_BOOL_ARRAY,
        AB_TYPE_CONTROL,
        AB_TYPE_COUNTER,
        AB_TYPE_FLOAT32,
        AB_TYPE_FLOAT64,
        AB_TYPE_INT8,
        AB_TYPE_INT16,
        AB_TYPE_INT32,
        AB_TYPE_INT64,
        AB_TYPE_STRING,
        AB_TYPE_SHORT_STRING,
        AB_TYPE_TIMER,
        AB_TYPE_TAG_ENTRY,  /* not a real AB type, but a pseudo type for AB's internal tag entry. */
        AB_TYPE_TAG_UDT,    /* as above, but for UDTs. */
        AB_TYPE_TAG_RAW     /* raw CIP tag */
    };

    public delegate PlcTag tag_constructor(attr attributes, /*TagCallbackFunc*/ tag_extended_callback_func tag_callback_func, Object userdata);

    public struct tag_type_map_t {
        public String protocol;
        public String make;
        public String family;
        public String model;
        //const tag_create_function tag_constructor;
        //public delegate PlcTag tag_constructor();
        public tag_constructor tag_Constructor;
        public tag_type_map_t(String protocol, String make, String family, String model, tag_constructor tc)
        {
            this.protocol = protocol;
            this.make = make;
            this.family = family;
            this.model = model;
            this.tag_Constructor = tc;
        }
    }

    /* byte ordering */

    public class tag_byte_order_t
    {
        /* set if we allocated this specifically for the tag. */
        public bool is_allocated;

        /* string type and ordering. */
        public bool str_is_defined;
        public bool str_is_counted;
        public bool str_is_fixed_length;
        public bool str_is_zero_terminated;
        public bool str_is_byte_swapped;

        public UInt16 str_pad_to_multiple_bytes;
        public UInt16 str_count_word_bytes;
        public UInt16 str_max_capacity;
        public UInt16 str_total_length;
        public UInt16 str_pad_bytes;

        public Int16[] int16_order = new Int16[2];
        public Int16[] int32_order = new Int16[4];
        public Int16[] int64_order = new Int16[8];

        public Int16[] float32_order = new Int16[4];
        public Int16[] float64_order = new Int16[8];
    };


    public class PlcTag: Status, IDisposable
    {
        // Para Overridear
        virtual public int abort() { return 0; }
        virtual public int read() { return 0; }
        virtual public int status() { return 0; }
        virtual public int tickler() { return 0; }
        virtual public int write() { return 0; }

        virtual public int wake_plc() { return 0; }


        const int TAG_ID_MASK = 0xFFFFFFF;

        //public static HashSet<PlcTag> tags = new HashSet<PlcTag>();
        public static Dictionary<Int32, PlcTag> tags = new Dictionary<Int32, PlcTag> ();
        
        static Random rand = new Random();
                
        public bool is_bit;
        public bool tag_is_dirty;
        public bool read_in_flight;
        public bool read_complete;
        public bool write_in_flight;
        public bool write_complete;
        public bool skip_tickler;
        public bool had_created_event;
        public bool event_creation_complete;
        public bool event_deletion_started;
        public bool event_operation_aborted;
        public bool event_read_started;
        public bool event_read_complete_enable;
        public bool event_read_complete;
        public bool event_write_started;
        public bool event_write_complete_enable;
        public bool event_write_complete;
        public byte event_creation_complete_status;
        public byte event_deletion_started_status;
        public byte event_operation_aborted_status;
        public byte event_read_started_status;
        public byte event_read_complete_status;
        public byte event_write_started_status;
        public byte event_write_complete_status;
        public int status_;
        public Int16 bit;
        public Int16 connection_group_id;
        public Int32 size;
        public Int32 tag_id;
        public Int32 auto_sync_read_ms;
        public Int32 auto_sync_write_ms;
        public byte[] data;
        public tag_byte_order_t byte_order = null; // new tag_byte_order_t();
        public mutex_t ext_mutex=new mutex_t();
        public mutex_t api_mutex=new mutex_t();
        public Cond tag_cond_wait;
        // public tag_vtable_t vtable;
        public tag_extended_callback_func callback;
        public Object userdata;
        public Int64 read_cache_expire;
        public Int64 read_cache_ms;
        public Int64 auto_sync_next_read;
        public Int64 auto_sync_next_write;
        public object value;

        public PlcTag()
        {
            initialize_modules();
        }
        public PlcTag(attr attribs, tag_extended_callback_func tag_callback_func, Object userdata):this()
        {
            plc_tag_generic_init_tag(attribs, tag_callback_func, userdata);

            /* set up the read cache config. */
  /*HR*VER          long read_cache_ms = Attr.attr_get_int(attribs, "read_cache_ms", 0);
            if (read_cache_ms < 0)
            {
                //pdebug(DEBUG_WARN, "read_cache_ms value must be positive, using zero.");
                read_cache_ms = 0;
            }

//*HR*VER            read_cache_expire = 0;
//*HR*VER            this.read_cache_ms = read_cache_ms;

            /* set up any automatic read/write */
/*HR*VER            auto_sync_read_ms = Attr.attr_get_int(attribs, "auto_sync_read_ms", 0);
            if (auto_sync_read_ms < 0)
            {
                //pdebug(DEBUG_WARN, "auto_sync_read_ms value must be positive!");
                //attr_destroy(attribs);
                //rc_dec(tag);
                //return null;  // PLCTAG_ERR_BAD_PARAM;
                throw new Exception("auto_sync_read_ms value must be positive!");
            }
            else if (auto_sync_read_ms > 0)
            {
                /* how many periods did we already pass? */
                // int64_t periods = (time_ms() / tag->auto_sync_read_ms);
                // tag->auto_sync_next_read = (periods + 1) * tag->auto_sync_read_ms;
                /* start some time in the future, but with random jitter. */
/*HR*VER                auto_sync_next_read = System.DateTime.Now.Millisecond +
                    rand.Next() % auto_sync_read_ms;
            }

            auto_sync_write_ms = Attr.attr_get_int(attribs, "auto_sync_write_ms", 0);
            if (auto_sync_write_ms < 0)
            {
                //pdebug(DEBUG_WARN, "auto_sync_write_ms value must be positive!");
                //attr_destroy(attribs);
                //rc_dec(tag);
                //return null; // PLCTAG_ERR_BAD_PARAM;
                throw new Exception("auto_sync_write_ms value must be positive!");
            }
            else
            {
                auto_sync_next_write = 0;
            }

            /* set up the tag byte order if there are any overrides. */
/*HR*VER            int rc = set_tag_byte_order(attribs);
            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_WARN, "Unable to correctly set tag data byte order: %s!", plc_tag_decode_error(rc));
                //attr_destroy(attribs);
                //rc_dec(tag);
                //return null; // rc;
                throw new Exception ("Unable to correctly set tag data byte order: %s!");
            }
*/
        }

        public static tag_type_map_t[] tag_type_map = new tag_type_map_t[] {
            /* System tags */
            new tag_type_map_t(null, "system", "library", null, SystemTag.system_tag_create),

            /* Allen-Bradley PLCs */
            new tag_type_map_t( "ab-eip", null, null, null, AbTag.ab_tag_create),
            new tag_type_map_t( "ab_eip", null, null, null, AbTag.ab_tag_create),
        //    new tag_type_map_t( "modbus-tcp", NULL, NULL, NULL, mb_tag_create),
        //    new tag_type_map_t( "modbus_tcp", NULL, NULL, NULL, mb_tag_create)
        };
        
        public static PlcTag plc_tag_create_(string attrib_str, int timeout)
        {
            return plc_tag_create_ex_(attrib_str, null, null, timeout);
        }

        public static Int32 plc_tag_create(string attrib_str, int timeout)
        {
            return plc_tag_create_ex(attrib_str, null, null, timeout);
        }


        public delegate void TagCallbackFunc/*(int hwnd, int lParam);*/ (int tag_id, int Event, int status, object userdata);
        public delegate void tag_extended_callback_func /*(int32_t tag_id, int event, int status, void* user_data)*/
            (int tag_id, int Event, int status, Object user_data);

        public static Int32 plc_tag_create_ex(string attrib_str, callback_func_ex/*TagCallbackFunc tag_extended_callback_func*/ tag_callback_func, object userdata, int timeout)
        {
            return plc_tag_create_(attrib_str, timeout).tag_id;
        }
        public static PlcTag plc_tag_create_ex_(string attrib_str, /*TagCallbackFunc*/ tag_extended_callback_func tag_callback_func, object userdata, int timeout)
        {
            PlcTag tag = null;

            int id = PLCTAG_ERR_OUT_OF_BOUNDS;
            attr attribs = null;
            int rc = PLCTAG_STATUS_OK;
            int read_cache_ms = 0;
            tag_constructor tag_constructor = null;
            int debug_level = -1;

            /* check the arguments */

            if (timeout < 0)
            {
                //pdebug(DEBUG_WARN, "Timeout must not be negative!");
                return null; // PLCTAG_ERR_BAD_PARAM;
            }

            if ((attrib_str == null) || attrib_str.Length == 0)
            {
                //pdebug(DEBUG_WARN, "Tag attribute string is null or zero length!");
                return null; // PLCTAG_ERR_TOO_SMALL;
            }

            attribs = Attr.attr_create_from_str(attrib_str);
            if (attribs == null)
            {
                //pdebug(DEBUG_WARN, "Unable to parse attribute string!");
                return null; // PLCTAG_ERR_BAD_DATA;
            }

            
            /*
             * create the tag, this is protocol specific.
             *
             * If this routine wants to keep the attributes around, it needs
             * to clone them.
             */
            tag_constructor = find_tag_create_func(attribs);

            if (tag_constructor == null)
            {
                //pdebug(DEBUG_WARN, "Tag creation failed, no tag constructor found for tag type!");
                //attr_destroy(attribs);
                return null; // PLCTAG_ERR_BAD_PARAM;
            }

            tag = tag_constructor(attribs, tag_callback_func, userdata);

            /*if (tag.get_status() != PLCTAG_STATUS_OK && tag.get_status() != PLCTAG_STATUS_PENDING)
            {
                int tag_status = tag.get_status();

                //pdebug(DEBUG_WARN, "Warning, %s error found while creating tag!", plc_tag_decode_error(tag_status));

                //attr_destroy(attribs);
                //rc_dec(tag);

                return null; // tag_status;
            }*/

            
            /* map the tag to a tag ID */
            //id = add_tag_lookup(tag);
            lock(tags)
                tags.Add(tag.GetHashCode(), tag);
            tag.tag_id = tag.GetHashCode();

            tag.wake_plc();


            /* get the tag status. */
            //rc = tag.vtable.status(tag);
            rc = tag.status();

            /* check to see if there was an error during tag creation. */
            if (rc != PLCTAG_STATUS_OK && rc != PLCTAG_STATUS_PENDING)
            {
                //pdebug(DEBUG_WARN, "Error %s while trying to create tag!", plc_tag_decode_error(rc));
                /*if (tag.vtable.abort!=null)
                {
                    tag.vtable.abort(tag);
                }*/
                tag.abort();

                /* remove the tag from the hashtable. */
                /*critical_block(tag_lookup_mutex) {
                    hashtable_remove(tags, (int64_t)tag->tag_id);
                }*/
                lock(tags)
                    tags.Remove(tag.tag_id);

                //rc_dec(tag);
                return null; // rc;
            }

            //pdebug(DEBUG_DETAIL, "Tag status after creation is %s.", plc_tag_decode_error(rc));

            /*
            * if there is a timeout, then wait until we get
            * an error or we timeout.
            */
            if (timeout > 0 && rc == PLCTAG_STATUS_PENDING)
            {
                Int64 start_time = Alpiste.Utils.Milliseconds.ms(); // time_ms();
                Int64 end_time = start_time + timeout;

                /* wake up the tickler in case it is needed to create the tag. */
                plc_tag_tickler_wake();

                /* we loop as long as we have time left to wait. */
                do
                {
                    Int64 timeout_left = end_time - Alpiste.Utils.Milliseconds.ms(); //time_ms();

                    /* clamp the timeout left to non-negative int range. */
                    if (timeout_left < 0)
                    {
                        timeout_left = 0;
                    }

                    if (timeout_left > Int16.MaxValue /* INT_MAX*/)
                    {
                        timeout_left = 100; /* MAGIC, only wait 100ms in this weird case. */
                    }

                    /* wait for something to happen */
                    tag.tag_cond_wait = new Cond();
                    rc = tag.tag_cond_wait.cond_wait(/*tag_cond_wait,*/ (int)timeout_left);

                    if (rc != PLCTAG_STATUS_OK)
                    {
                        //pdebug(DEBUG_WARN, "Error %s while waiting for tag creation to complete!", plc_tag_decode_error(rc));
                        /*if (tag->vtable->abort)
                        {
                            tag->vtable->abort(tag);
                        }*/
                        tag.abort();

                        /* remove the tag from the hashtable. */
                        //critical_block(tag_lookup_mutex) {
                        //       hashtable_remove(tags, (int64_t)tag->tag_id);
                        lock (tags)
                            tags.Remove(tag.tag_id);
                        //   }

                        //rc_dec(tag);
                        return null; // rc;
                    }

                    /* get the tag status. */
                    rc = tag.status();  // vtable.status(tag);

                    /* check to see if there was an error during tag creation. */
                    if (rc != PLCTAG_STATUS_OK && rc != PLCTAG_STATUS_PENDING)
                    {
                        //pdebug(DEBUG_WARN, "Error %s while trying to create tag!", plc_tag_decode_error(rc));
                        /*if (tag.vtable.abort!=null)
                        {
                            tag.vtable.abort(tag);
                        }*/
                        tag.abort();

                        /* remove the tag from the hashtable. */
                        /*critical_block(tag_lookup_mutex) {
                            hashtable_remove(tags, (int64_t)tag->tag_id);
                        }*/
                        lock (tags)
                            tags.Remove(tag.tag_id);

                        //rc_dec(tag);
                        throw new Exception("Timeout expired");
                        //return null; // rc;
                    }
                } while (rc == PLCTAG_STATUS_PENDING && Alpiste.Utils.Milliseconds.ms() /*    time_ms()*/ > end_time);

                /* clear up any remaining flags.  This should be refactored. */
                tag.read_in_flight = false;
                tag.write_in_flight = false;

                /* raise create event. */
                //tag_raise_event(tag, PLCTAG_EVENT_CREATED, (int8_t)rc);


                //pdebug(DEBUG_INFO, "tag set up elapsed time %" PRId64 "ms", time_ms() - start_time);
            }
            
            if (rc!= PLCTAG_STATUS_OK)
            {
                tag.abort();
                lock (tags)
                    tags.Remove(tag.tag_id);
                throw new Exception("Couldn't create the tag.");
            }
            /* dispatch any outstanding events. */
            tag.plc_tag_generic_handle_event_callbacks();

            //pdebug(DEBUG_INFO, "Done.");

            return tag; //id;
        }

        /*
         * find_tag_create_func()
         *
         * Find an appropriate tag creation function.  This scans through the array
         * above to find a matching tag creation type.  The first match is returned.
         * A passed set of options will match when all non-null entries in the list
         * match.  This means that matches must be ordered from most to least general.
         *
         * Note that the protocol is used if it exists otherwise, the make family and
         * model will be used.
         */

        public static tag_constructor find_tag_create_func(attr attributes)
        {
            int i = 0;
            String protocol = Attr.attr_get_str(attributes, "protocol", null);
            String make = Attr.attr_get_str(attributes, "make", Attr.attr_get_str(attributes, "manufacturer", null));
            String family = Attr.attr_get_str(attributes, "family", null);
            String model = Attr.attr_get_str(attributes, "model", null);
            //int num_entries = (sizeof(tag_type_map) / sizeof(tag_type_map[0]));
            int num_entries = tag_type_map.Length;

            /* if protocol is set, then use it to match. */
            if (protocol != null && protocol.Length > 0)
            {
                for (i = 0; i < num_entries; i++)
                {
                    if ((tag_type_map[i].protocol != null)
                        && tag_type_map[i].protocol == protocol)
                    {
                        //pdebug(DEBUG_INFO, "Matched protocol=%s", protocol);
                        return tag_type_map[i].tag_Constructor;
                    }
                }
            }
            else
            {
                /* match make/family/model */
                for (i = 0; i < num_entries; i++)
                {
                    if ((tag_type_map[i].make != null)
                        && make != null && tag_type_map[i].make == make)
                    {
                        //pdebug(DEBUG_INFO, "Matched make=%s", make);
                        if ((tag_type_map[i].family != null))
                        {
                            if (family != null && tag_type_map[i].family == family)
                            {
                                //pdebug(DEBUG_INFO, "Matched make=%s family=%s", make, family);
                                if (tag_type_map[i].model != null)
                                {
                                    if (model != null && tag_type_map[i].model == model)
                                    {
                                        //pdebug(DEBUG_INFO, "Matched make=%s family=%s model=%s", make, family, model);
                                        return tag_type_map[i].tag_Constructor;
                                    }
                                }
                                else
                                {
                                    /* matches until a NULL */
                                    //pdebug(DEBUG_INFO, "Matched make=%s family=%s model=NULL", make, family);
                                    return tag_type_map[i].tag_Constructor;
                                }
                            }
                        }
                        else
                        {
                            /* matched until a NULL, so we matched */
                            //pdebug(DEBUG_INFO, "Matched make=%s family=NULL model=NULL", make);
                            return tag_type_map[i].tag_Constructor;
                        }
                    }
                }
            }

            /* no match */
            return null;
        }

        /*****************************************************************************************************
 *****************************  Support routines for extra indirection *******************************
 ****************************************************************************************************/

        int set_tag_byte_order(/*plc_tag_p tag,*/ attr attribs)

        {
            int use_default = 1;

            //pdebug(DEBUG_INFO, "Starting.");

            /* the default values are already set in the tag. */

            /* check for overrides. */
            if (Attr.attr_get_str(attribs, "int16_byte_order", null) != null)
            {
                use_default = 0;
            }

            if (Attr.attr_get_str(attribs, "int32_byte_order", null) != null)
            {
                use_default = 0;
            }

            if (Attr.attr_get_str(attribs, "int64_byte_order", null) != null)
            {
                use_default = 0;
            }

            if (Attr.attr_get_str(attribs, "float32_byte_order", null) != null)
            {
                use_default = 0;
            }

            if (Attr.attr_get_str(attribs, "float64_byte_order", null) != null)
            {
                use_default = 0;
            }

            if (Attr.attr_get_str(attribs, "str_is_counted", null) != null)
            {
                use_default = 0;
            }

            if (Attr.attr_get_str(attribs, "str_is_fixed_length", null) != null)
            {
                use_default = 0;
            }

            if (Attr.attr_get_str(attribs, "str_is_zero_terminated", null) != null)
            {
                use_default = 0;
            }

            if (Attr.attr_get_str(attribs, "str_is_byte_swapped", null) != null)
            {
                use_default = 0;
            }

            if (Attr.attr_get_str(attribs, "str_count_word_bytes", null) != null)
            {
                use_default = 0;
            }

            if (Attr.attr_get_str(attribs, "str_max_capacity", null) != null)
            {
                use_default = 0;
            }

            if (Attr.attr_get_str(attribs, "str_total_length", null) != null)
            {
                use_default = 0;
            }

            if (Attr.attr_get_str(attribs, "str_pad_bytes", null) != null)
            {
                use_default = 0;
            }

            if (Attr.attr_get_str(attribs, "str_pad_to_multiple_bytes_EXPERIMENTAL", null) != null)
            {
                use_default = 0;
            }

            /* if we need to override something, build a new byte order structure. */
            if (use_default == 0)
            {
                String byte_order_str = null;
                int str_param = 0;
                int rc = PLCTAG_STATUS_OK;
                //tag_byte_order_t* new_byte_order = mem_alloc((int)(unsigned int)sizeof(*(tag->byte_order)));
                tag_byte_order_t new_byte_order = new tag_byte_order_t();

                /*if (!new_byte_order)
                {
                    //pdebug(DEBUG_WARN, "Unable to allocate byte order struct for tag!");
                    return PLCTAG_ERR_NO_MEM;
                }

                /* copy the defaults. */
                //*new_byte_order = *(tag->byte_order);

                /* replace the old byte order. */
                byte_order = new_byte_order;

                /* mark it as allocated so that we free it later. */
                byte_order.is_allocated = true;

                /* 16-bit ints. */
                byte_order_str = Attr.attr_get_str(attribs, "int16_byte_order", null);
                if (byte_order_str != null)
                {
                    //pdebug(DEBUG_DETAIL, "Override byte order int16_byte_order=%s", byte_order_str);

                    rc = check_byte_order_str(byte_order_str, 2);
                    if (rc != PLCTAG_STATUS_OK)
                    {
                        //pdebug(DEBUG_WARN, "Byte order string int16_byte_order, \"%s\", is illegal or malformed.", byte_order_str);
                        return rc;
                    }

                    /* strange gyrations to make the compiler happy.   MSVC will probably complain. */
                    byte_order.int16_order[0] = (short)((byte)byte_order_str[0] & 0x01); // (int)(unsigned int)(((unsigned int)byte_order_str[0] - (unsigned int)('0')) &0x01);
                    byte_order.int16_order[1] = (short)((byte)byte_order_str[1] & 0x01); // (int)(unsigned int)(((unsigned int)byte_order_str[1] - (unsigned int)('0')) &0x01);
                }

                /* 32-bit ints. */
                byte_order_str = Attr.attr_get_str(attribs, "int32_byte_order", null);
                if (byte_order_str != null)
                {
                    //pdebug(DEBUG_DETAIL, "Override byte order int32_byte_order=%s", byte_order_str);

                    rc = check_byte_order_str(byte_order_str, 4);
                    if (rc != PLCTAG_STATUS_OK)
                    {
                        //pdebug(DEBUG_WARN, "Byte order string int32_byte_order, \"%s\", is illegal or malformed.", byte_order_str);
                        return rc;
                    }

                    byte_order.int32_order[0] = (short)((byte)byte_order_str[0] & 0x03); // (int)(unsigned int)(((unsigned int)byte_order_str[0] - (unsigned int)('0')) &0x03);
                    byte_order.int32_order[1] = (short)((byte)byte_order_str[1] & 0x03); //(int)(unsigned int)(((unsigned int)byte_order_str[1] - (unsigned int)('0')) &0x03);
                    byte_order.int32_order[2] = (short)((byte)byte_order_str[2] & 0x03); //(int)(unsigned int)(((unsigned int)byte_order_str[2] - (unsigned int)('0')) &0x03);
                    byte_order.int32_order[3] = (short)((byte)byte_order_str[3] & 0x03); //(int)(unsigned int)(((unsigned int)byte_order_str[3] - (unsigned int)('0')) &0x03);
                }

                /* 64-bit ints. */
                byte_order_str = Attr.attr_get_str(attribs, "int64_byte_order", null);
                if (byte_order_str != null)
                {
                    //pdebug(DEBUG_DETAIL, "Override byte order int64_byte_order=%s", byte_order_str);

                    rc = check_byte_order_str(byte_order_str, 8);
                    if (rc != PLCTAG_STATUS_OK)
                    {
                        //pdebug(DEBUG_WARN, "Byte order string int64_byte_order, \"%s\", is illegal or malformed.", byte_order_str);
                        return rc;
                    }

                    byte_order.int64_order[0] = (short)((byte)byte_order_str[0] & 0x07); //(int)(unsigned int)(((unsigned int)byte_order_str[0] - (unsigned int)('0')) &0x07);
                    byte_order.int64_order[1] = (short)((byte)byte_order_str[0] & 0x07); //(int)(unsigned int)(((unsigned int)byte_order_str[1] - (unsigned int)('0')) &0x07);
                    byte_order.int64_order[2] = (short)((byte)byte_order_str[0] & 0x07); //(int)(unsigned int)(((unsigned int)byte_order_str[2] - (unsigned int)('0')) &0x07);
                    byte_order.int64_order[3] = (short)((byte)byte_order_str[0] & 0x07); //(int)(unsigned int)(((unsigned int)byte_order_str[3] - (unsigned int)('0')) &0x07);
                    byte_order.int64_order[4] = (short)((byte)byte_order_str[0] & 0x07); //(int)(unsigned int)(((unsigned int)byte_order_str[4] - (unsigned int)('0')) &0x07);
                    byte_order.int64_order[5] = (short)((byte)byte_order_str[0] & 0x07); //(int)(unsigned int)(((unsigned int)byte_order_str[5] - (unsigned int)('0')) &0x07);
                    byte_order.int64_order[6] = (short)((byte)byte_order_str[0] & 0x07); //(int)(unsigned int)(((unsigned int)byte_order_str[6] - (unsigned int)('0')) &0x07);
                    byte_order.int64_order[7] = (short)((byte)byte_order_str[0] & 0x07); //(int)(unsigned int)(((unsigned int)byte_order_str[7] - (unsigned int)('0')) &0x07);
                }

                /* 32-bit floats. */
                byte_order_str = Attr.attr_get_str(attribs, "float32_byte_order", null);
                if (byte_order_str != null)
                {
                    //pdebug(DEBUG_DETAIL, "Override byte order float32_byte_order=%s", byte_order_str);

                    rc = check_byte_order_str(byte_order_str, 4);
                    if (rc != PLCTAG_STATUS_OK)
                    {
                        //pdebug(DEBUG_WARN, "Byte order string float32_byte_order, \"%s\", is illegal or malformed.", byte_order_str);
                        return rc;
                    }

                    byte_order.float32_order[0] = (short)((byte)byte_order_str[0] & 0x03); //(int)(unsigned int)(((unsigned int)byte_order_str[0] - (unsigned int)('0')) &0x03);
                    byte_order.float32_order[1] = (short)((byte)byte_order_str[1] & 0x03); //(int)(unsigned int)(((unsigned int)byte_order_str[1] - (unsigned int)('0')) &0x03);
                    byte_order.float32_order[2] = (short)((byte)byte_order_str[2] & 0x03); //(int)(unsigned int)(((unsigned int)byte_order_str[2] - (unsigned int)('0')) &0x03);
                    byte_order.float32_order[3] = (short)((byte)byte_order_str[3] & 0x03); //(int)(unsigned int)(((unsigned int)byte_order_str[3] - (unsigned int)('0')) &0x03);

                }
                /* 64-bit floats */
                byte_order_str = Attr.attr_get_str(attribs, "float64_byte_order", null);
                if (byte_order_str != null)
                {
                    //pdebug(DEBUG_DETAIL, "Override byte order float64_byte_order=%s", byte_order_str);

                    rc = check_byte_order_str(byte_order_str, 8);
                    if (rc != PLCTAG_STATUS_OK)
                    {
                        //pdebug(DEBUG_WARN, "Byte order string float64_byte_order, \"%s\", is illegal or malformed.", byte_order_str);
                        return rc;
                    }

                    byte_order.float64_order[0] = (short)((byte)byte_order_str[0] & 0x07); //(int)(unsigned int)(((unsigned int)byte_order_str[0] - (unsigned int)('0')) &0x07);
                    byte_order.float64_order[1] = (short)((byte)byte_order_str[1] & 0x07); //(int)(unsigned int)(((unsigned int)byte_order_str[1] - (unsigned int)('0')) &0x07);
                    byte_order.float64_order[2] = (short)((byte)byte_order_str[2] & 0x07); //(int)(unsigned int)(((unsigned int)byte_order_str[2] - (unsigned int)('0')) &0x07);
                    byte_order.float64_order[3] = (short)((byte)byte_order_str[3] & 0x07); //(int)(unsigned int)(((unsigned int)byte_order_str[3] - (unsigned int)('0')) &0x07);
                    byte_order.float64_order[4] = (short)((byte)byte_order_str[4] & 0x07); //(int)(unsigned int)(((unsigned int)byte_order_str[4] - (unsigned int)('0')) &0x07);
                    byte_order.float64_order[5] = (short)((byte)byte_order_str[5] & 0x07); //(int)(unsigned int)(((unsigned int)byte_order_str[5] - (unsigned int)('0')) &0x07);
                    byte_order.float64_order[6] = (short)((byte)byte_order_str[6] & 0x07); //(int)(unsigned int)(((unsigned int)byte_order_str[6] - (unsigned int)('0')) &0x07);
                    byte_order.float64_order[7] = (short)((byte)byte_order_str[7] & 0x07); //(int)(unsigned int)(((unsigned int)byte_order_str[7] - (unsigned int)('0')) &0x07);
                }

                /* string information. */

                /* is the string counted? */
                if (Attr.attr_get_str(attribs, "str_is_counted", null) != null)
                {
                    str_param = Attr.attr_get_int(attribs, "str_is_counted", 0);
                    if (str_param == 1 || str_param == 0)
                    {
                        byte_order.str_is_counted = (str_param == 1);
                        //    byte_order.str_is_counted = false; // (str_param != 0) ? 1 : 0);

                    }
                    else
                    {
                        //pdebug(DEBUG_WARN, "Tag string attribute str_is_counted must be missing, zero (0) or one (1)!");
                        return PLCTAG_ERR_BAD_PARAM;
                    }
                }

                /* is the string a fixed length? */
                if (Attr.attr_get_str(attribs, "str_is_fixed_length", null) != null)
                {
                    str_param = Attr.attr_get_int(attribs, "str_is_fixed_length", 0);
                    if (str_param == 1 || str_param == 0)
                    {
                        byte_order.str_is_fixed_length = (str_param == 1);

                        //tag->byte_order->str_is_fixed_length = (str_param ? 1 : 0);
                    }
                    else
                    {
                        //pdebug(DEBUG_WARN, "Tag string attribute str_is_fixed_length must be missing, zero (0) or one (1)!");
                        return PLCTAG_ERR_BAD_PARAM;
                    }
                }

                /* is the string zero terminated? */
                if (Attr.attr_get_str(attribs, "str_is_zero_terminated", null) != null)
                {
                    str_param = Attr.attr_get_int(attribs, "str_is_zero_terminated", 0);
                    if (str_param == 1 || str_param == 0)
                    {
                        byte_order.str_is_zero_terminated = (str_param == 1);
                        //tag->byte_order->str_is_zero_terminated = (str_param ? 1 : 0);
                    }
                    else
                    {
                        //pdebug(DEBUG_WARN, "Tag string attribute str_is_zero_terminated must be missing, zero (0) or one (1)!");
                        return PLCTAG_ERR_BAD_PARAM;
                    }
                }

                /* is the string byteswapped like PLC/5? */
                if (Attr.attr_get_str(attribs, "str_is_byte_swapped", null) != null)
                {
                    str_param = Attr.attr_get_int(attribs, "str_is_byte_swapped", 0);
                    if (str_param == 1 || str_param == 0)
                    {
                        byte_order.str_is_byte_swapped = (str_param == 1);
                        //tag->byte_order->str_is_byte_swapped = (str_param ? 1 : 0);
                    }
                    else
                    {
                        //pdebug(DEBUG_WARN, "Tag string attribute str_is_byte_swapped must be missing, zero (0) or one (1)!");
                        return PLCTAG_ERR_BAD_PARAM;
                    }
                }

                /* main string parameters. */

                /* how many bytes is the string count word? */
                if (Attr.attr_get_str(attribs, "str_count_word_bytes", null) != null)
                {
                    str_param = Attr.attr_get_int(attribs, "str_count_word_bytes", 0);
                    if (str_param == 0 || str_param == 1 || str_param == 2 || str_param == 4 || str_param == 8)
                    {
                        byte_order.str_count_word_bytes = (UInt16)str_param;
                        //tag->byte_order->str_count_word_bytes = (unsigned int)str_param;
                    }
                    else
                    {
                        //pdebug(DEBUG_WARN, "Tag string attribute str_count_word_bytes must be missing, 0, 1, 2, 4, or 8!");
                        return PLCTAG_ERR_BAD_PARAM;
                    }
                }

                /* What is the string maximum capacity */
                if (Attr.attr_get_str(attribs, "str_max_capacity", null) != null)
                {
                    str_param = Attr.attr_get_int(attribs, "str_max_capacity", 0);
                    if (str_param >= 0)
                    {
                        byte_order.str_max_capacity = (UInt16)str_param;
                    }
                    else
                    {
                        //pdebug(DEBUG_WARN, "Tag string attribute str_max_capacity must be missing, 0, or positive!");
                        return PLCTAG_ERR_BAD_PARAM;
                    }
                }

                /* What is the string total length */
                if (Attr.attr_get_str(attribs, "str_total_length", null) != null)
                {
                    str_param = Attr.attr_get_int(attribs, "str_total_length", 0);
                    if (str_param >= 0)
                    {
                        byte_order.str_total_length = (UInt16)str_param;
                    }
                    else
                    {
                        //pdebug(DEBUG_WARN, "Tag string attribute str_total_length must be missing, 0, or positive!");
                        return PLCTAG_ERR_BAD_PARAM;
                    }
                }

                /* What is the string padding length */
                if (Attr.attr_get_str(attribs, "str_pad_bytes", null) != null)
                {
                    str_param = Attr.attr_get_int(attribs, "str_pad_bytes", 0);
                    if (str_param >= 0)
                    {
                        byte_order.str_pad_bytes = (UInt16)str_param;
                    }
                    else
                    {
                        //pdebug(DEBUG_WARN, "Tag string attribute str_pad_bytes must be missing, 0, or positive!");
                        return PLCTAG_ERR_BAD_PARAM;
                    }
                }

                /* Should we pad the string to a multiple of 1 (no padding), 2, or 4 bytes. Adding padding causes issues when writing OmronNJ strings,
                    2 byte padding is required for certain AB PLCs*/
                if (Attr.attr_get_str(attribs, "str_pad_to_multiple_bytes_EXPERIMENTAL", null) != null)
                {
                    str_param = Attr.attr_get_int(attribs, "str_pad_to_multiple_bytes_EXPERIMENTAL", 0);
                    if (str_param == 0 || str_param == 1 || str_param == 2 || str_param == 4)
                    {
                        if (str_param == 0) { str_param = 1; } /* Padding to 0 bytes doesnt make much sense, so we overwride to 1 byte which means no padding */

                        byte_order.str_pad_to_multiple_bytes = (UInt16)str_param;
                    }
                    else
                    {
                        //pdebug(DEBUG_WARN, "Tag string attribute str_pad_to_multiple_bytes must be missing, 1, 2 or 4!");
                        return PLCTAG_ERR_BAD_PARAM;
                    }
                }

                /* now make sure that the combination of settings works. */

                /* if we have a counted string, we need the count! */
                if (byte_order.str_is_counted)
                {
                    if (byte_order.str_count_word_bytes == 0)
                    {
                        //pdebug(DEBUG_WARN, "If a string definition is counted, you must use both \"str_is_counted\" and \"str_count_word_bytes\" parameters!");
                        return PLCTAG_ERR_BAD_PARAM;
                    }
                }

                /* if we have a fixed length string, we need to know what the length is! */
                if (byte_order.str_is_fixed_length)
                {
                    if (byte_order.str_total_length == 0)
                    {
                        //pdebug(DEBUG_WARN, "If a string definition is fixed length, you must use both \"str_is_fixed_length\" and \"str_total_length\" parameters!");
                        return PLCTAG_ERR_BAD_PARAM;
                    }
                }

                /* check the total length. */
                if (byte_order.str_total_length > 0
                  && (byte_order.str_is_zero_terminated ? 1 : 0
                    + byte_order.str_max_capacity
                    + byte_order.str_count_word_bytes
                    + byte_order.str_pad_bytes)
                  > byte_order.str_total_length)
                {
                    //pdebug(DEBUG_WARN, "Tag string total length must be at least the sum of the other string components!");
                    return PLCTAG_ERR_BAD_PARAM;
                }

                /* Do we have enough of a definition for a string? */
                /* FIXME - This is probably not enough checking! */
                if (byte_order.str_is_counted || byte_order.str_is_zero_terminated)
                {
                    byte_order.str_is_defined = true;
                }
                else
                {
                    //pdebug(DEBUG_WARN, "Insufficient definitions found to support strings!");
                }
            }

            //pdebug(DEBUG_INFO, "Done.");

            return PLCTAG_STATUS_OK;
        }

        public static int check_byte_order_str(String byte_order, int length)
        {
            Int16[] taken = new Int16[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
            int byte_order_len = byte_order.Length;

            //pdebug(DEBUG_DETAIL, "Starting.");

            /* check the size. */
            if (byte_order_len != length) {
                //pdebug(DEBUG_WARN, "Byte order string, \"%s\", must be %d characters long!", byte_order, length);
                return (byte_order_len < length ? PLCTAG_ERR_TOO_SMALL : PLCTAG_ERR_TOO_LARGE);
            }

            /* check each character. */
            for (int i = 0; i < byte_order_len; i++) {
                int val = 0;

                if (!isdigit(byte_order[i]) || byte_order[i] < '0' || byte_order[i] > '7') {
                    //pdebug(DEBUG_WARN, "Byte order string, \"%s\", must be only characters from '0' to '7'!", byte_order);
                    return PLCTAG_ERR_BAD_DATA;
                }

                /* get the numeric value. */
                val = byte_order[i] - '0';

                if (val < 0 || val > (length - 1))
                {
                    //pdebug(DEBUG_WARN, "Byte order string, \"%s\", must only values from 0 to %d!", byte_order, (length - 1));
                    return PLCTAG_ERR_BAD_DATA;
                }

                if (taken[val] != 0)
                {
                    //pdebug(DEBUG_WARN, "Byte order string, \"%s\", must use each digit exactly once!", byte_order);
                    return PLCTAG_ERR_BAD_DATA;
                }

                taken[val] = 1;
            }

            // pdebug(DEBUG_DETAIL, "Done.");

            return PLCTAG_STATUS_OK;
        }

        public static bool isdigit(char c)
        {
            if (c < '0' || c > '9')
            {
                return false;
            }
            else
            { return true; }
        }

        public int plc_tag_generic_init_tag(attr attribs, tag_extended_callback_func tag_callback_func, object userData)
        {
            int rc = PLCTAG_STATUS_OK;

            //pdebug(DEBUG_INFO, "Starting.");

            /* get the connection group ID here rather than in each PLC specific tag type. */
            connection_group_id = (Int16)Attr.attr_get_int(attribs, "connection_group_id", 0);
            if (connection_group_id < 0 || connection_group_id > 32767)
            {
                //pdebug(DEBUG_WARN, "Connection group ID must be between 0 and 32767, inclusive, but was %d!", tag->connection_group_id);
                return PLCTAG_ERR_OUT_OF_BOUNDS;
            }

            /* do this early so that events can be raised early. */
            callback = (tag_extended_callback_func)tag_callback_func;
            this.userdata = userData;

            //pdebug(DEBUG_INFO, "Done.");

            return rc;
        }

        


        public static Cond tag_tickler_wait = new Cond(); //null;
        public static int plc_tag_tickler_wake(/*const char* func, int line_num*/)
        {
            int rc = PLCTAG_STATUS_OK;

            //pdebug(DEBUG_DETAIL, "Starting. Called from %s:%d.", func, line_num);

            if (tag_tickler_wait == null) {
                //pdebug(DEBUG_WARN, "Called from %s:%d when tag tickler condition var is NULL!", func, line_num);
                return PLCTAG_ERR_NULL_PTR;
            }

            //rc = cond_signal(tag_tickler_wait);
            tag_tickler_wait.cond_signal();


            if (rc != PLCTAG_STATUS_OK) {
                //pdebug(DEBUG_WARN, "Error %s trying to signal condition variable in call from %s:%d", plc_tag_decode_error(rc), func, line_num);
                return rc;
            }

            //pdebug(DEBUG_DETAIL, "Done. Called from %s:%d.", func, line_num);

            return rc;
        }



        /*
         * Initialize the library.  This is called in a threadsafe manner and
         * only called once.
         */

        static bool library_terminating = false;

        static Thread tag_tickler_thread = new Thread(new ThreadStart(ref tag_tickler_func));

        const int TAG_TICKLER_TIMEOUT_MS = 100;
        const int TAG_TICKLER_TIMEOUT_MIN_MS = 10;
        static Int64 tag_tickler_wait_timeout_end = 0;
        static mutex_t tag_lookup_mutex = null;




        static public int lib_init()
        {
            int rc = PLCTAG_STATUS_OK;

            //pdebug(DEBUG_INFO, "Starting.");


            /*atomic_set(&library_terminating, 0);*/
            library_terminating = false;

            //pdebug(DEBUG_INFO, "Setting up global library data.");

            //pdebug(DEBUG_INFO, "Creating tag hashtable.");
            /*if ((tags = hashtable_create(INITIAL_TAG_TABLE_SIZE)) == NULL)
            { /* MAGIC */
            /*    pdebug(DEBUG_ERROR, "Unable to create tag hashtable!");
                return PLCTAG_ERR_NO_MEM;
            }*/

            //pdebug(DEBUG_INFO, "Creating tag hashtable mutex.");
            tag_lookup_mutex = new mutex_t();
            /*rc = mutex_create((mutex_p*)&tag_lookup_mutex);
            if (rc != PLCTAG_STATUS_OK)
            {
                pdebug(DEBUG_ERROR, "Unable to create tag hashtable mutex!");
            }*/

            //pdebug(DEBUG_INFO, "Creating tag condition variable.");
            /*rc = cond_create((cond_p*)&tag_tickler_wait);
            if (rc != PLCTAG_STATUS_OK)
            {
                pdebug(DEBUG_ERROR, "Unable to create tag condition var!");
            }*/

            //pdebug(DEBUG_INFO, "Creating tag tickler thread.");

            //rc = thread_create(&tag_tickler_thread, tag_tickler_func, 32 * 1024, NULL);

            tag_tickler_thread = new Thread(new ThreadStart(ref tag_tickler_func));
            tag_tickler_thread.Start();
            
            /*if (rc != PLCTAG_STATUS_OK)
            {
                pdebug(DEBUG_ERROR, "Unable to create tag tickler thread!");
            }*/

            //pdebug(DEBUG_INFO, "Done.");

            return rc;
        }

        public static void lib_teardown()
        {
            //pdebug(DEBUG_INFO, "Tearing down library.");

            library_terminating = true;

            
            //tag_tickler_wait.cond_signal();

            /*if (tag_tickler_wait)
            {
                //pdebug(DEBUG_INFO, "Signaling tag tickler condition var.");
                cond_signal(tag_tickler_wait);
            }*/
            
            while (tag_tickler_thread.ThreadState != System.Threading.ThreadState.Stopped)
            {
                Thread.Sleep(100);
                tag_tickler_wait.cond_signal();
            }
            /*if (tag_tickler_thread)
            {
                pdebug(DEBUG_INFO, "Tearing down tag tickler thread.");
                thread_join(tag_tickler_thread);
                thread_destroy(&tag_tickler_thread);
                tag_tickler_thread = NULL;
            }*/

            /*if (tag_tickler_wait)
            {
                pdebug(DEBUG_INFO, "Tearing down tag tickler condition var.");
                cond_destroy(&tag_tickler_wait);
                tag_tickler_wait = NULL;
            }*/

            /*if (tag_lookup_mutex)
            {
                pdebug(DEBUG_INFO, "Tearing down tag lookup mutex.");
                mutex_destroy(&tag_lookup_mutex);
                tag_lookup_mutex = NULL;
            }*/

            /*if (tags)
            {
                pdebug(DEBUG_INFO, "Destroying tag hashtable.");
                hashtable_destroy(tags);
                tags = NULL;
            }*/

            lock (tags)
            {
                foreach (PlcTag tag in tags.Values)
                {
                    tag.abort();
                }
            }
            tags.Clear();

            library_terminating = false;
            library_initialized = false;

            //pdebug(DEBUG_INFO, "Done.");
        }

        static void tag_tickler_func_()
        {
            //(void)arg;

            //debug_set_tag_id(0);

            //pdebug(DEBUG_INFO, "Starting.");

            while (!library_terminating)
            {
                int max_index = 0;
                Int64 timeout_wait_ms = TAG_TICKLER_TIMEOUT_MS;

                /* what is the maximum time we will wait until */
                tag_tickler_wait_timeout_end = Alpiste.Utils.Milliseconds.ms() /*time_ms()*/ + timeout_wait_ms;
  //*ELIM*              tag_lookup_mutex.mutex_lock();
  //*ELIM*              //Monitor.Enter(tag_lookup_mutex);
  //*ELIM*              {
                    //critical_block(tag_lookup_mutex) {
  //*ELIM*                  lock(tags)
  //*ELIM*                      max_index = tags.Count; //hashtable_capacity(tags);
                                            //}
   //*ELIM*             }
                //Monitor.Exit(tag_lookup_mutex);
   //*ELIM*             tag_lookup_mutex.mutex_unlock();

                PlcTag[] tags_array = new PlcTag[tags.Count];
                lock (tags)
                {
                    //tags_array = tags.Values.ToArray();
                    for (int i = 0; i < tags.Count; i++)
                    {
                        tags_array[i]=tags.ElementAt(i).Value;
                    }
                }

   //*ELIM*             for (int i = 0; i < max_index; i++)
                foreach(PlcTag tag_ in tags_array)
                {
   //*ELIM*                 PlcTag tag = null;

                    //critical_block(tag_lookup_mutex) {
                    //Monitor.Enter(tag_lookup_mutex); {
    //*ELIM*                tag_lookup_mutex.mutex_lock(); { 
                        /* look up the max index again. it may have changed. */
    //*ELIM*                    lock(tags)
    //*ELIM*                        max_index = tags.Count; // hashtable_capacity(tags);

    //*ELIM*                    if (i < max_index)
    //*ELIM*                    {
                            //tag = hashtable_get_index(tags, i);
     //*ELIM*                       lock (tags)
     //*ELIM*                           tag = tags.ElementAt(i);

                            /*if (tag)
                            {
                                debug_set_tag_id(tag->tag_id);
                                tag = rc_inc(tag);
                            }*/
     //*ELIM*                   }
     //*ELIM*                   else
     //*ELIM*                   {
                            //debug_set_tag_id(0);
     //*ELIM*                       tag = null;
     //*ELIM*                   }
     //*ELIM*               }
     //*ELIM*               tag_lookup_mutex.mutex_unlock();
                    //Monitor.Exit(tag_lookup_mutex);


     //*ELIM*               if (tag != null)
     //*ELIM*               {
                        //debug_set_tag_id(tag->tag_id);

                        if (!tag_.skip_tickler)
                        {
                            //pdebug(DEBUG_DETAIL, "Tickling tag %d.", tag->tag_id);

                            /* try to hold the tag API mutex while all this goes on. */
                            //if (mutex_try_lock(tag->api_mutex) == PLCTAG_STATUS_OK)
                            if (tag_.api_mutex.mutex_try_lock() == PLCTAG_STATUS_OK)
                            {
                                tag_.plc_tag_generic_tickler(/*tag*/);

                                /* call the tickler function if we can. */
                                //if (tag->vtable && tag->vtable->tickler)
                                //{
                                //    /* call the tickler on the tag. */
                                //    tag->vtable->tickler(tag);
                                tag_.tickler();

                                    if (tag_.read_complete)
                                    {
                                        tag_.read_complete = false;
                                        tag_.read_in_flight = false;

                                        //tag->event_read_complete = 1;
      //*HR*                                  tag_raise_event(tag, PLCTAG_EVENT_READ_COMPLETED, tag->status);

                                        /* wake immediately */
                                        plc_tag_tickler_wake();
                                    //cond_signal(tag->tag_cond_wait);
                                    tag_.tag_cond_wait.cond_signal();
                                    }

                                    if (tag_.write_complete)
                                    {
                                        tag_.write_complete = false;
                                        tag_.write_in_flight = false;
                                        tag_.auto_sync_next_write = 0;

                                        // tag->event_write_complete = 1;
   //*HR*                                        tag_raise_event(tag, PLCTAG_EVENT_WRITE_COMPLETED, tag->status);

                                        /* wake immediately */
                                        plc_tag_tickler_wake();
                                        //cond_signal(tag->tag_cond_wait);
                                        tag_.tag_cond_wait.cond_signal( );
                                    }
                                //}

                                /* wake up earlier if the time until the next write wake up is sooner. */
                                if (tag_.auto_sync_next_write != null && tag_.auto_sync_next_write < tag_tickler_wait_timeout_end)
                                {
                                    tag_tickler_wait_timeout_end = tag_.auto_sync_next_write;
                                }

                                /* wake up earlier if the time until the next read wake up is sooner. */
                                if (tag_.auto_sync_next_read!=null && tag_.auto_sync_next_read < tag_tickler_wait_timeout_end)
                                {
                                    tag_tickler_wait_timeout_end = tag_.auto_sync_next_read;
                                }

                                /* we are done with the tag API mutex now. */
                                //mutex_unlock(tag->api_mutex);
                                tag_.api_mutex.mutex_unlock();

                                /* call callbacks */
     //*HR*                           plc_tag_generic_handle_event_callbacks(tag);
                            }
                            else
                            {
                                //pdebug(DEBUG_DETAIL, "Skipping tag as it is already locked.");
                            }

                        }
                        else
                        {
                            //pdebug(DEBUG_DETAIL, "Tag has its own tickler.");
                        }

                        // pdebug(DEBUG_DETAIL, "Current time %" PRId64 ".", time_ms());
                        // pdebug(DEBUG_DETAIL, "Time to wake %" PRId64 ".", tag_tickler_wait_timeout_end);
                        // pdebug(DEBUG_DETAIL, "Auto read time %" PRId64 ".", tag->auto_sync_next_read);
                        // pdebug(DEBUG_DETAIL, "Auto write time %" PRId64 ".", tag->auto_sync_next_write);

                        //debug_set_tag_id(0);
   //*ELIM**                 }

                    /*if (tag)
                    {
                        rc_dec(tag);
                    }*/

                    //debug_set_tag_id(0);
                }

                if (tag_tickler_wait!=null)
                {
                    Int64 time_to_wait = tag_tickler_wait_timeout_end - Alpiste.Utils.Milliseconds.ms() /*time_ms()*/;
                    int wait_rc = PLCTAG_STATUS_OK;

                    if (time_to_wait < TAG_TICKLER_TIMEOUT_MIN_MS)
                    {
                        time_to_wait = TAG_TICKLER_TIMEOUT_MIN_MS;
                    }

                    if (time_to_wait > 0)
                    {
                        //wait_rc = cond_wait(tag_tickler_wait, (int)time_to_wait);
                        wait_rc = tag_tickler_wait.cond_wait((int)time_to_wait);
                        if (wait_rc == PLCTAG_ERR_TIMEOUT)
                        {
                            //pdebug(DEBUG_DETAIL, "Tag tickler thread timed out waiting for something to do.");
                        }
                    }
                    else
                    {
                        //pdebug(DEBUG_DETAIL, "Not waiting as time to wake is in the past.");
                    }
                }
            }

            //debug_set_tag_id(0);

            //pdebug(DEBUG_INFO, "Terminating.");

            //THREAD_RETURN(0);
        }

        //public delegate void EventDelegate(int status);

       // public event EventHandler PLCTAG_EVENT_READ_COMPLETED;
        public event EventDelegate PLCTAG_EVENT_READ_COMPLETED;
        public event EventDelegate PLCTAG_EVENT_WRITE_COMPLETED;

        /*protected virtual void on_PLCTAG_EVENT_READ_COMPLETED(EventArgs e)
        {
            PLCTAG_EVENT_READ_COMPLETED?.Invoke(this, e);
        }*/

        static void tag_tickler_func()
        {
            while (!library_terminating)
            {
                /* what is the maximum time we will wait until */
                tag_tickler_wait_timeout_end = Alpiste.Utils.Milliseconds.ms() + TAG_TICKLER_TIMEOUT_MS;
            
                PlcTag[] tags_array;
                lock (tags)
                    tags_array = tags.Values.ToArray();

                foreach (PlcTag tag in tags_array)
                {
                    if (!tag.skip_tickler)
                    {
                        /* try to hold the tag API mutex while all this goes on. */
                        if (tag.api_mutex.mutex_try_lock() == PLCTAG_STATUS_OK)
                        {
                            tag.plc_tag_generic_tickler();

                            tag.tickler();

                            if (tag.read_complete)
                            {
                                tag.read_complete = false;
                                tag.read_in_flight = false;

                                //tag->event_read_complete = 1;
                                //*HR*                                  tag_raise_event(tag, PLCTAG_EVENT_READ_COMPLETED, tag->status);
                                //tag.PLCTAG_EVENT_READ_COMPLETED?.Invoke(tag.status);
                                tag.event_read_complete = true;
                                tag.event_read_complete_status = (byte) tag.status_;
                                tag.event_read_complete_enable = false;


                                /* wake immediately */
                                //plc_tag_tickler_wake();
                                tag_tickler_wait.cond_signal();
                                tag.tag_cond_wait.cond_signal();
                            }

                            if (tag.write_complete)
                            {
                                tag.write_complete = false;
                                tag.write_in_flight = false;
                                tag.auto_sync_next_write = 0;

                                // tag->event_write_complete = 1;
                                //*HR*                                        tag_raise_event(tag, PLCTAG_EVENT_WRITE_COMPLETED, tag->status);
                                //tag.PLCTAG_EVENT_WRITE_COMPLETED?.Invoke(tag.status);
                                tag.event_write_complete = true;
                                tag.event_write_complete_status = (byte) tag.status_;
                                tag.event_write_complete_enable = false;


                                /* wake immediately */
                                //plc_tag_tickler_wake();
                                tag_tickler_wait.cond_signal();
                                tag.tag_cond_wait.cond_signal();
                            }
                    
                            /* wake up earlier if the time until the next write wake up is sooner. */
                            if (tag.auto_sync_next_write != 0 && tag.auto_sync_next_write < tag_tickler_wait_timeout_end)
                            {
                                tag_tickler_wait_timeout_end = tag.auto_sync_next_write;
                            }

                            /* wake up earlier if the time until the next read wake up is sooner. */
                            if (tag.auto_sync_next_read != 0 && tag.auto_sync_next_read < tag_tickler_wait_timeout_end)
                            {
                                tag_tickler_wait_timeout_end = tag.auto_sync_next_read;
                            }

                            /* we are done with the tag API mutex now. */
                            tag.api_mutex.mutex_unlock();

                            /* call callbacks */
                            tag.plc_tag_generic_handle_event_callbacks(/*tag*/);
                        }
                        else
                        {
                            //pdebug(DEBUG_DETAIL, "Skipping tag as it is already locked.");
                        }

                    }
                    else
                    {
                        //pdebug(DEBUG_DETAIL, "Tag has its own tickler.");
                    }

                    // pdebug(DEBUG_DETAIL, "Current time %" PRId64 ".", time_ms());
                    // pdebug(DEBUG_DETAIL, "Time to wake %" PRId64 ".", tag_tickler_wait_timeout_end);
                    // pdebug(DEBUG_DETAIL, "Auto read time %" PRId64 ".", tag->auto_sync_next_read);
                    // pdebug(DEBUG_DETAIL, "Auto write time %" PRId64 ".", tag->auto_sync_next_write);

                    
                }

        //        if (tag_tickler_wait != null)
        //        {
                    Int64 time_to_wait = tag_tickler_wait_timeout_end - Alpiste.Utils.Milliseconds.ms() /*time_ms()*/;
                    int wait_rc = PLCTAG_STATUS_OK;

                    if (time_to_wait < TAG_TICKLER_TIMEOUT_MIN_MS)
                    {
                        time_to_wait = TAG_TICKLER_TIMEOUT_MIN_MS;
                    }

                    if (time_to_wait > 0)
                    {
                        wait_rc = tag_tickler_wait.cond_wait((int)time_to_wait);
                        if (wait_rc == PLCTAG_ERR_TIMEOUT)
                        {
                            //pdebug(DEBUG_DETAIL, "Tag tickler thread timed out waiting for something to do.");
                        }
                    }
                    else
                    {
                        //pdebug(DEBUG_DETAIL, "Not waiting as time to wake is in the past.");
                    }
       //         }
            }
        }

        public delegate void EventDelegate(int status, object userdata);

        // public event EventHandler PLCTAG_EVENT_READ_COMPLETED;
        public event EventDelegate PLCTAG_EVENT_CREATED;
        public event EventDelegate PLCTAG_EVENT_READ_STARTED;
        public event EventDelegate PLCTAG_EVENT_WRITE_STARTED;
        public event EventDelegate PLCTAG_EVENT_ABORTED;
        public event EventDelegate PLCTAG_EVENT_DESTROYED;
        public void plc_tag_generic_handle_event_callbacks(/*plc_tag_p tag*/)
        {
            //critical_block(tag->api_mutex)
            lock(this.api_mutex)
                
                {
                /* call the callbacks outside the API mutex. */
                //if (tag && tag->callback)
                if (callback!=null)
                {
                    //debug_set_tag_id(tag->tag_id);

                    /* trigger this if there is any other event. Only once. */
                    if (event_creation_complete)
                    {
                        //pdebug(DEBUG_DETAIL, "Tag creation complete with status %s.", plc_tag_decode_error(tag->event_creation_complete_status));
                        //tag->callback(tag->tag_id, PLCTAG_EVENT_CREATED, tag->event_creation_complete_status, tag.userdata);
                        PLCTAG_EVENT_CREATED?.Invoke(event_creation_complete_status, userdata);
                        event_creation_complete = false;
                        event_creation_complete_status = PLCTAG_STATUS_OK;
                    }

                    /* was there a read start? */
                    if (event_read_started)
                    {
                        //pdebug(DEBUG_DETAIL, "Tag read started with status %s.", plc_tag_decode_error(tag->event_read_started_status));
                        //tag->callback(tag->tag_id, PLCTAG_EVENT_READ_STARTED, tag->event_read_started_status, tag->userdata);
                        PLCTAG_EVENT_READ_STARTED?.Invoke(event_read_started_status, userdata);
                        event_read_started = false;
                        event_read_started_status = PLCTAG_STATUS_OK;
                    }

                    /* was there a write start? */
                    if (event_write_started)
                    {
                        //pdebug(DEBUG_DETAIL, "Tag write started with status %s.", plc_tag_decode_error(tag->event_write_started_status));
                        //tag->callback(tag->tag_id, PLCTAG_EVENT_WRITE_STARTED, tag->event_write_started_status, tag->userdata);
                        PLCTAG_EVENT_WRITE_STARTED?.Invoke(event_write_started_status,userdata);
                        event_write_started = false;
                        event_write_started_status = PLCTAG_STATUS_OK;
                    }

                    /* was there an abort? */
                    if (event_operation_aborted)
                    {
                        //pdebug(DEBUG_DETAIL, "Tag operation aborted with status %s.", plc_tag_decode_error(tag->event_operation_aborted_status));
                        //tag->callback(tag->tag_id, PLCTAG_EVENT_ABORTED, tag->event_operation_aborted_status, tag->userdata);
                        PLCTAG_EVENT_ABORTED?.Invoke(event_operation_aborted_status, userdata);
                        event_operation_aborted = false;
                        event_operation_aborted_status = PLCTAG_STATUS_OK;
                    }

                    /* was there a read completion? */
                    if (event_read_complete)
                    {
                        //pdebug(DEBUG_DETAIL, "Tag read completed with status %s.", plc_tag_decode_error(tag->event_read_complete_status));
                        //tag->callback(tag->tag_id, PLCTAG_EVENT_READ_COMPLETED, tag->event_read_complete_status, tag->userdata);
                        PLCTAG_EVENT_READ_COMPLETED?.Invoke(event_read_complete_status, userdata);
                        event_read_complete = false;
                        event_read_complete_status = PLCTAG_STATUS_OK;
                    }

                    /* was there a write completion? */
                    if (event_write_complete)
                    {
                        //pdebug(DEBUG_DETAIL, "Tag write completed with status %s.", plc_tag_decode_error(tag->event_write_complete_status));
                        //tag->callback(tag->tag_id, PLCTAG_EVENT_WRITE_COMPLETED, tag->event_write_complete_status, tag->userdata);
                        PLCTAG_EVENT_WRITE_COMPLETED?.Invoke(event_write_complete_status, userdata);
                        event_write_complete = false;
                        event_write_complete_status = PLCTAG_STATUS_OK;
                    }

                    /* do this last so that we raise all other events first. we only start deletion events. */
                    if (event_deletion_started)
                    {
                        //pdebug(DEBUG_DETAIL, "Tag deletion started with status %s.", plc_tag_decode_error(tag->event_creation_complete_status));
                        //tag->callback(tag->tag_id, PLCTAG_EVENT_DESTROYED, tag->event_deletion_started_status, tag->userdata);
                        PLCTAG_EVENT_DESTROYED?.Invoke(event_deletion_started_status, userdata);
                        event_deletion_started = false;
                        event_deletion_started_status = PLCTAG_STATUS_OK;
                    }

                    //debug_set_tag_id(0);
                }
            } /* end of API mutex critical area. */
        }



        /***************************************************************************
         ******************************* Mutexes ***********************************
         **************************************************************************/

        public class mutex_t
        {
            //Object h_mutex;
            //bool initialized;

            /*mutex_t()
            {
                //pdebug(DEBUG_DETAIL, "Starting.");

                /*if (*m)
            {
                pdebug(DEBUG_WARN, "Called with non-NULL pointer!");
            }

            *m = (struct mutex_t *)mem_alloc(sizeof(struct mutex_t));
    if(! *m) {
        pdebug(DEBUG_WARN, "null mutex pointer!");
        return PLCTAG_ERR_NULL_PTR;
    } */

                /* set up the mutex */
                /*
                h_mutex = CreateMutex(
                            NULL,                   /* default security attributes  */
                /*            FALSE,                  /* initially not owned          */
                /*            NULL);                  /* unnamed mutex                */

                /*if(!(* m)->h_mutex) {
            mem_free(*m);
            * m = NULL;
        pdebug(DEBUG_WARN, "Error initializing mutex!");
            return PLCTAG_ERR_MUTEX_INIT;
                }*/

                //initialized = true;

                //pdebug(DEBUG_DETAIL, "Done.");

                //return PLCTAG_STATUS_OK;
  /*          }*/



            public int mutex_lock()
            {
                //DWORD dwWaitResult = true;

                //pdebug(DEBUG_SPEW, "locking mutex %p, called from %s:%d.", m, func, line);

                /*if (!m)
    {
        pdebug(DEBUG_WARN, "null mutex pointer.");
        return PLCTAG_ERR_NULL_PTR;
    }*/

                /*if (!m->initialized)
                {
                    return PLCTAG_ERR_MUTEX_INIT;
                }*/

                //dwWaitResult = false; // ~WAIT_OBJECT_0;

                /* FIXME - This will potentially hang forever! */
                /*while (dwWaitResult != WAIT_OBJECT_0)
                {
                    //dwWaitResult = WaitForSingleObject(m->h_mutex, INFINITE);
                    
                }*/
                Monitor.Enter(this);

                return PLCTAG_STATUS_OK;
            }



            public int mutex_try_lock()
            {
                //DWORD dwWaitResult = 0;

                //pdebug(DEBUG_SPEW, "trying to lock mutex %p, called from %s:%d.", m, func, line);

                /*if (!m)
                {
                    pdebug(DEBUG_WARN, "null mutex pointer.");
                    return PLCTAG_ERR_NULL_PTR;
                }*/

                /*if (!m->initialized)
                {
                return PLCTAG_ERR_MUTEX_INIT;
                }*/

                //dwWaitResult = WaitForSingleObject(m->h_mutex, 0);


                //if (dwWaitResult == WAIT_OBJECT_0)
                if (Monitor.TryEnter(this, 0))
                {
                    /* we got the lock */
                    return PLCTAG_STATUS_OK;
                }
                else
                {
                    return PLCTAG_ERR_MUTEX_LOCK;
                }
            }

            public int mutex_unlock(/*const char* func, int line, mutex_p m*/)
            {
                //pdebug(DEBUG_SPEW, "unlocking mutex %p, called from %s:%d.", m, func, line);

                /*if (!m)
                {
                    pdebug(DEBUG_WARN, "null mutex pointer.");
                    return PLCTAG_ERR_NULL_PTR;
                }

                if (!m->initialized)
                {
                    return PLCTAG_ERR_MUTEX_INIT;
                }

                if (!ReleaseMutex(m->h_mutex))
                {
                    /*pdebug("error unlocking mutex.");
                    return PLCTAG_ERR_MUTEX_UNLOCK;
                }*/

                Monitor.Exit(this);

                //pdebug("Done.");

                return PLCTAG_STATUS_OK;
            }

            /*int mutex_destroy(mutex_p* m)
{
    pdebug(DEBUG_DETAIL, "destroying mutex %p", m);

    if (!m || !*m)
    {
        pdebug(DEBUG_WARN, "null mutex pointer.");
        return PLCTAG_ERR_NULL_PTR;
    }

    CloseHandle((*m)->h_mutex);

    mem_free(*m);

    *m = NULL;

    pdebug(DEBUG_DETAIL, "Done.");

    return PLCTAG_STATUS_OK;
}*/

        }

        /*
         * plc_tag_generic_tickler
         *
         * This implements the protocol-independent tickling functions such as handling
         * automatic tag operations and callbacks.
         */

        void plc_tag_generic_tickler_(/*plc_tag_p tag*/)
        {
            /*if (tag)
            {*/
                //debug_set_tag_id(tag->tag_id);

                //pdebug(DEBUG_DETAIL, "Tickling tag %d.", tag->tag_id);

                /* if this tag has automatic writes, then there are many things we should check */
            if (auto_sync_write_ms > 0)
            {
                    /* has the tag been written to? */
                    if (tag_is_dirty)
                    {
                        /* abort any in flight read if the tag is dirty. */
                        if (read_in_flight)
                        {
                        //if (tag->vtable && tag->vtable->abort)
                        //{
                        //    tag->vtable->abort(tag);
                        //}
                            abort();

                            //pdebug(DEBUG_DETAIL, "Aborting in-flight automatic read!");

                            read_complete = false;
                            read_in_flight = false;

                            /* TODO - should we report an ABORT event here? */
                            //tag->event_operation_aborted = 1;
      //*HR*                      tag_raise_event(tag, PLCTAG_EVENT_ABORTED, PLCTAG_ERR_ABORT);
                        }

                        /* have we already done something about automatic reads? */
                        if (auto_sync_next_write == 0)
                        {
                            /* we need to queue up a new write. */
                            auto_sync_next_write = Alpiste.Utils.Milliseconds.ms() /*time_ms()*/ + auto_sync_write_ms;

                            //pdebug(DEBUG_DETAIL, "Queueing up automatic write in %dms.", tag->auto_sync_write_ms);
                        }
                        else if (!write_in_flight && auto_sync_next_write <= Alpiste.Utils.Milliseconds.ms() /*time_ms()*/)
                        {
                            //pdebug(DEBUG_DETAIL, "Triggering automatic write start.");

                            /* clear out any outstanding reads. */
                            if (read_in_flight)
                            {
                                //if (tag->vtable && tag->vtable->abort)
                                //{
                                //    tag->vtable->abort(tag);
                                abort();
                                //}

                                read_in_flight = false;
                            }

                            tag_is_dirty = false;
                            write_in_flight = true;
                            auto_sync_next_write = 0;

                            //if (tag->vtable && tag->vtable->write)
                            //{
                                status_ = /*(int8_t)tag->vtable->*/write(/*tag*/);
                            //}

                            // tag->event_write_started = 1;
        //*HR*                    tag_raise_event(tag, PLCTAG_EVENT_WRITE_STARTED, tag->status);
                        }
                    }
            //}

                /* if this tag has automatic reads, we need to check that state too. */
                if (auto_sync_read_ms > 0)
                {
                    Int64 current_time = Alpiste.Utils.Milliseconds.ms() /*time_ms()*/;

                    // /* spread these out randomly to avoid too much clustering. */
                    // if(tag->auto_sync_next_read == 0) {
                    //     tag->auto_sync_next_read = current_time - (rand() % tag->auto_sync_read_ms);
                    // }

                    /* do we need to read? */
                    if (auto_sync_next_read < current_time)
                    {
                        /* make sure that we do not have an outstanding read or write. */
                        if (!read_in_flight && !tag_is_dirty && !write_in_flight)
                        {
                            Int64 periods = 0;

                            //pdebug(DEBUG_DETAIL, "Triggering automatic read start.");

                            read_in_flight = true;

                            //if (tag->vtable && tag->vtable->read)
                            //{
                                status_ = /*(int8_t)tag->vtable->*/read(/*tag*/);
                            //}

                            // tag->event_read_started = 1;
     //*HR*                       tag_raise_event(tag, PLCTAG_EVENT_READ_STARTED, tag->status);

                            /*
                            * schedule the next read.
                            *
                            * Note that there will be some jitter.  In that case we want to skip
                            * to the next read time that is a whole multiple of the read period.
                            *
                            * This keeps the jitter from slowly moving the polling cycle.
                            *
                            * Round up to the next period.
                            */
                            periods = (current_time - auto_sync_next_read + (auto_sync_read_ms - 1)) / auto_sync_read_ms;

                            /* warn if we need to skip more than one period. */
                            if (periods > 1)
                            {
                                //pdebug(DEBUG_WARN, "Skipping %" PRId64 " periods of %" PRId32 "ms.", periods, tag->auto_sync_read_ms);
                            }

                            auto_sync_next_read += (periods * auto_sync_read_ms);
                            //pdebug(DEBUG_DETAIL, "Scheduling next read at time %" PRId64 ".", tag->auto_sync_next_read);
                        }
                        else
                        {
                            //pdebug(DEBUG_SPEW, "Unable to start read tag->read_in_flight=%d, tag->tag_is_dirty=%d, tag->write_in_flight=%d!", tag->read_in_flight, tag->tag_is_dirty, tag->write_in_flight);
                        }
                    }
                }
            }
            else
            {
                //pdebug(DEBUG_WARN, "Called with null tag pointer!");
            }

            //pdebug(DEBUG_DETAIL, "Done.");

            //debug_set_tag_id(0);
        }

        void plc_tag_generic_tickler()
        {
            if (auto_sync_write_ms > 0)
            {
                /* has the tag been written to? */
                if (tag_is_dirty)
                {
                    /* abort any in flight read if the tag is dirty. */
                    if (read_in_flight)
                    {
                        abort();

                        //pdebug(DEBUG_DETAIL, "Aborting in-flight automatic read!");

                        read_complete = false;
                        read_in_flight = false;

                        /* TODO - should we report an ABORT event here? */
                        //tag->event_operation_aborted = 1;
                        //*HR*                      tag_raise_event(tag, PLCTAG_EVENT_ABORTED, PLCTAG_ERR_ABORT);
                    }

                    /* have we already done something about automatic reads? */
                    if (auto_sync_next_write == 0)
                    {
                        /* we need to queue up a new write. */
                        auto_sync_next_write = Alpiste.Utils.Milliseconds.ms() + auto_sync_write_ms;

                        //pdebug(DEBUG_DETAIL, "Queueing up automatic write in %dms.", tag->auto_sync_write_ms);
                    }
                    else if (!write_in_flight && auto_sync_next_write <= Alpiste.Utils.Milliseconds.ms())
                    {
                        //pdebug(DEBUG_DETAIL, "Triggering automatic write start.");

                        /* clear out any outstanding reads. */
                        if (read_in_flight)
                        {
                            abort();
                            
                            read_in_flight = false;
                        }

                        tag_is_dirty = false;
                        write_in_flight = true;
                        auto_sync_next_write = 0;

                        status_ = write();
                        
                        // tag->event_write_started = 1;
                        //*HR*                    tag_raise_event(tag, PLCTAG_EVENT_WRITE_STARTED, tag->status);
                    }
                }

                /* if this tag has automatic reads, we need to check that state too. */
                if (auto_sync_read_ms > 0)
                {
                    Int64 current_time = Alpiste.Utils.Milliseconds.ms() /*time_ms()*/;

                    // /* spread these out randomly to avoid too much clustering. */
                    // if(tag->auto_sync_next_read == 0) {
                    //     tag->auto_sync_next_read = current_time - (rand() % tag->auto_sync_read_ms);
                    // }

                    /* do we need to read? */
                    if (auto_sync_next_read < current_time)
                    {
                        /* make sure that we do not have an outstanding read or write. */
                        if (!read_in_flight && !tag_is_dirty && !write_in_flight)
                        {
                            Int64 periods = 0;

                            //pdebug(DEBUG_DETAIL, "Triggering automatic read start.");

                            read_in_flight = true;

                            status_ = read();
                    
                            // tag->event_read_started = 1;
                            //*HR*                       tag_raise_event(tag, PLCTAG_EVENT_READ_STARTED, tag->status);

                            /*
                            * schedule the next read.
                            *
                            * Note that there will be some jitter.  In that case we want to skip
                            * to the next read time that is a whole multiple of the read period.
                            *
                            * This keeps the jitter from slowly moving the polling cycle.
                            *
                            * Round up to the next period.
                            */
                            periods = (current_time - auto_sync_next_read + (auto_sync_read_ms - 1)) / auto_sync_read_ms;

                            /* warn if we need to skip more than one period. */
                            if (periods > 1)
                            {
                                //pdebug(DEBUG_WARN, "Skipping %" PRId64 " periods of %" PRId32 "ms.", periods, tag->auto_sync_read_ms);
                            }

                            auto_sync_next_read += (periods * auto_sync_read_ms);
                            //pdebug(DEBUG_DETAIL, "Scheduling next read at time %" PRId64 ".", tag->auto_sync_next_read);
                        }
                        else
                        {
                            //pdebug(DEBUG_SPEW, "Unable to start read tag->read_in_flight=%d, tag->tag_is_dirty=%d, tag->write_in_flight=%d!", tag->read_in_flight, tag->tag_is_dirty, tag->write_in_flight);
                        }
                    }
                }
            }
            else
            {
                //pdebug(DEBUG_WARN, "Called with null tag pointer!");
            }

            //pdebug(DEBUG_DETAIL, "Done.");

            //debug_set_tag_id(0);
        }


        /*
         * initialize_modules() is called the first time any kind of tag is
         * created.  It will be called before the tag creation routines are
         * run.
         */

        static mutex_t lib_mutex = new mutex_t();
        static bool library_initialized = false;
        private bool disposedValue;

        static int initialize_modules()
        {
            int rc = PLCTAG_STATUS_OK;

            {
                /*
                * guard library initialization with a mutex.
                *
                * This prevents busy waiting as would happen with just a spin lock.
                */
                lib_mutex.mutex_lock();
                {
                    if (!library_initialized)
                    {
                        //pdebug(DEBUG_INFO, "Initializing library modules.");
                        rc = lib_init();

                        //pdebug(DEBUG_INFO, "Initializing AB module.");
                        if (rc == PLCTAG_STATUS_OK)
                        {
 //*HR*                           rc = ab_init();
                        }

                        //pdebug(DEBUG_INFO, "Initializing Modbus module.");
                        if (rc == PLCTAG_STATUS_OK)
                        {
//*HR*                            rc = mb_init();
                        }

                        //pdebug(DEBUG_INFO, "Initializing Omron module.");
                        if (rc == PLCTAG_STATUS_OK)
                        {
//*HR*                            rc = omron_init();
                        }

                        /* hook the destructor */
//*HR*                        atexit(plc_tag_shutdown);

                        /* do this last */
                        library_initialized = true;

                        //pdebug(DEBUG_INFO, "Done initializing library modules.");
                    }
                }
                lib_mutex.mutex_unlock();
            }

            //pdebug(DEBUG_INFO, "Done.");

            return rc;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: eliminar el estado administrado (objetos administrados)

                }

                abort();
                lock (tags) tags.Remove(this.tag_id);
                
                if (tags.Count == 0)
                    {
                        lib_teardown();
                    }
                
                // TODO: liberar los recursos no administrados (objetos no administrados) y reemplazar el finalizador
                // TODO: establecer los campos grandes como NULL
                disposedValue = true;
            }
        }

        // // TODO: reemplazar el finalizador solo si "Dispose(bool disposing)" tiene código para liberar los recursos no administrados
        // ~PlcTag()
        // {
        //     // No cambie este código. Coloque el código de limpieza en el método "Dispose(bool disposing)".
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // No cambie este código. Coloque el código de limpieza en el método "Dispose(bool disposing)".
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


        public static int plc_tag_destroy(Int32 tag_id)
        {
            PlcTag tag = null;

            //debug_set_tag_id((int)tag_id);

            //pdebug(DEBUG_INFO, "Starting.");

            if (tag_id <= 0 || tag_id >= TAG_ID_MASK)
            {
                //pdebug(DEBUG_WARN, "Called with zero or invalid tag!");
                return PLCTAG_ERR_NULL_PTR;
            }

            /*critical_block(tag_lookup_mutex) {
                tag = hashtable_remove(tags, tag_id);
            }*/
         
            lock (tags)
            {
                //tag = tags.GetValueOrDefault(tag_id);
                bool b = tags.TryGetValue(tag_id, out tag);
                if (!b) tag = null;

                if (tag !=null)
                {
                    tags.Remove(tag_id);
                }
                /*foreach (PlcTag tag_ in tags)
                {
                    if (tag_.tag_id == tag_id)
                    {
                        tag = tag_;
                        tags.Remove(tag_);
                        break;
                    }
                }*/
                
            }
               

            /*if (!tag)
            {
                pdebug(DEBUG_WARN, "Called with non-existent tag!");
                return PLCTAG_ERR_NOT_FOUND;
            }*/

            /* abort anything in flight */
            //pdebug(DEBUG_DETAIL, "Aborting any in-flight operations.");

            tag.api_mutex.mutex_lock();
            //critical_block(tag->api_mutex)
            {
                /*if (tag->vtable && tag->vtable->abort)
                {
                    /* Force a clean up. */
                /*    tag->vtable->abort(tag);
                }
                */
                tag.abort();


                //tag_raise_event(tag, PLCTAG_EVENT_DESTROYED, PLCTAG_STATUS_OK);

            }
            tag.api_mutex.mutex_unlock();

            /* wake the tickler */
            plc_tag_tickler_wake();

            tag.plc_tag_generic_handle_event_callbacks();

            /* release the reference outside the mutex. */
            //rc_dec(tag);

            //pdebug(DEBUG_INFO, "Done.");

            //debug_set_tag_id(0);
            tag.Dispose(); 
            
            return PLCTAG_STATUS_OK;
        }



        //*HR*  Para ser usado con Modbus

        //        int plc_tag_generic_wake_tag_impl(const char* func, int line_num, plc_tag_p tag)
        //{
        //    int rc = PLCTAG_STATUS_OK;
        //
        //    pdebug(DEBUG_DETAIL, "Starting. Called from %s:%d.", func, line_num);
        //
        //    if(!tag) {
        //        pdebug(DEBUG_WARN, "Called from %s:%d when tag is NULL!", func, line_num);
        //        return PLCTAG_ERR_NULL_PTR;
        //    }
        //
        //    if(!tag->tag_cond_wait) {
        //        pdebug(DEBUG_WARN, "Called from %s:%d when tag condition var is NULL!", func, line_num);
        //        return PLCTAG_ERR_NULL_PTR;
        //    }
        //
        //    rc = cond_signal(tag->tag_cond_wait);
        //    if(rc != PLCTAG_STATUS_OK) {
        //        pdebug(DEBUG_WARN, "Error %s trying to signal condition variable in call from %s:%d", plc_tag_decode_error(rc), func, line_num);
        //        return rc;
        //    }
        //
        //    pdebug(DEBUG_DETAIL, "Done. Called from %s:%d.", func, line_num);
        //
        //    return rc;
        //}

        /*
         * plc_tag_read()
         *
         * This function calls through the vtable in the passed tag to call
         * the protocol-specific implementation.  That starts the read operation.
         * If there is a timeout passed, then this routine waits for either
         * a timeout or an error.
         *
         * The status of the operation is returned.
         */

        public static int plc_tag_read(Int32 id, int timeout)
        {
            int rc = PLCTAG_STATUS_OK;
            PlcTag tag = lookup_tag(id);
            int is_done = 0;

            //pdebug(DEBUG_INFO, "Starting.");

            if (tag == null)
            {
                //pdebug(DEBUG_WARN, "Tag not found.");
                return PLCTAG_ERR_NOT_FOUND;
            }

            if (timeout < 0)
            {
                //pdebug(DEBUG_WARN, "Timeout must not be negative!");
                //rc_dec(tag);
                return PLCTAG_ERR_BAD_PARAM;
            }

            tag.api_mutex.mutex_lock();
            do {
                //critical_block(tag->api_mutex) {
                //tag_raise_event(tag, PLCTAG_EVENT_READ_STARTED, PLCTAG_STATUS_OK);
                tag.plc_tag_generic_handle_event_callbacks();

                /* check read cache, if not expired, return existing data. */
                if (tag.read_cache_expire > Alpiste.Utils.Milliseconds.ms())
                {
                    //pdebug(DEBUG_INFO, "Returning cached data.");
                    rc = PLCTAG_STATUS_OK;
                    is_done = 1;
                    break;
                }

                if (tag.read_in_flight || tag.write_in_flight)
                {
                    //pdebug(DEBUG_WARN, "An operation is already in flight!");
                    rc = PLCTAG_ERR_BUSY;
                    is_done = 1;
                    break;
                }

                if (tag.tag_is_dirty)
                {
                    //pdebug(DEBUG_WARN, "Tag has locally updated data that will be overwritten!");
                    rc = PLCTAG_ERR_BUSY;
                    is_done = 1;
                    break;
                }

                tag.read_in_flight = true;
                tag.status_ = PLCTAG_STATUS_PENDING;

                /* clear the condition var */
                //cond_clear(tag->tag_cond_wait);
                tag.tag_cond_wait.cond_clear();
                tag.tag_cond_wait.debug = true;
                /* the protocol implementation does not do the timeout. */
                //if (tag.vtable && tag.vtable->read)
                //{
                //rc = tag->vtable->read(tag);
                rc = tag.read();
                //}
                /*else
                {
                    pdebug(DEBUG_WARN, "Attempt to call read on a tag that does not support reads.");
                    rc = PLCTAG_ERR_NOT_IMPLEMENTED;
                }*/

                /* if not pending then check for success or error. */
                if (rc != PLCTAG_STATUS_PENDING)
                {
                    if (rc != PLCTAG_STATUS_OK)
                    {
                        /* not pending and not OK, so error. Abort and clean up. */

                        //pdebug(DEBUG_WARN, "Response from read command returned error %s!", plc_tag_decode_error(rc));

                        //if (tag->vtable && tag->vtable->abort)
                        //{
                        //    tag->vtable->abort(tag);
                        tag.abort();
                        //}
                    }

                    tag.read_in_flight = false;
                    is_done = 1;
                    break;
                }
            } while (false);
            tag.api_mutex.mutex_unlock();

            /*
             * if there is a timeout, then wait until we get
             * an error or we timeout.
             */

            if ((is_done ==0 ) && timeout > 0)
            {
                Int64 start_time = Alpiste.Utils.Milliseconds.ms();
                Int64 end_time = start_time + timeout;

                /* wake up the tickler in case it is needed to read the tag. */
                plc_tag_tickler_wake();

                /* we loop as long as we have time left to wait. */
                do
                {
                    Int64 timeout_left = end_time - Alpiste.Utils.Milliseconds.ms();

                    /* clamp the timeout left to non-negative int range. */
                    if (timeout_left < 0)
                    {
                        timeout_left = 0;
                    }

                    if (timeout_left > Int32.MaxValue)
                    {
                        timeout_left = 100; /* MAGIC, only wait 100ms in this weird case. */
                    }

                    /* wait for something to happen */
                    //rc = cond_wait(tag->tag_cond_wait, (int)timeout_left);
                    rc = tag.tag_cond_wait.cond_wait((int)timeout_left);

                    if (rc != PLCTAG_STATUS_OK)
                    {
                        //pdebug(DEBUG_WARN, "Error %s while waiting for tag read to complete!", plc_tag_decode_error(rc));
                        plc_tag_abort(id);

                        break;
                    }

                    /* get the tag status. */
                    rc = plc_tag_status(id);

                    /* check to see if there was an error during tag read. */
                    if (rc != PLCTAG_STATUS_OK && rc != PLCTAG_STATUS_PENDING)
                    {
                        //pdebug(DEBUG_WARN, "Error %s while trying to read tag!", plc_tag_decode_error(rc));
                        plc_tag_abort(id);
                    }
                } while (rc == PLCTAG_STATUS_PENDING && Alpiste.Utils.Milliseconds.ms() < end_time);

                /* the read is not in flight anymore. */
                tag.api_mutex.mutex_lock();
                do {

                    //critical_block(tag->api_mutex) {
                    tag.read_in_flight = false;
                    tag.read_complete = false;
                    is_done = 1;
                    //tag_raise_event(tag, PLCTAG_EVENT_READ_COMPLETED, (int8_t)rc);
                } while (false);
                tag.api_mutex.mutex_unlock();

                //pdebug(DEBUG_INFO, "elapsed time %" PRId64 "ms", (time_ms() - start_time));
            }

            if (rc == PLCTAG_STATUS_OK)
            {
                /* set up the cache time.  This works when read_cache_ms is zero as it is already expired. */
                tag.read_cache_expire = Alpiste.Utils.Milliseconds.ms() + tag.read_cache_ms;
            }

            /* fire any events that are pending. */
            tag.plc_tag_generic_handle_event_callbacks();

            //rc_dec(tag);

            //pdebug(DEBUG_INFO, "Done");

            return rc;
        }


        public static PlcTag lookup_tag(Int32 tag_id)
            {
                PlcTag tag = null;

                tag_lookup_mutex.mutex_lock();
                //critical_block(tag_lookup_mutex) {
                { 
                    
                    //tag = hashtable_get(tags, (int64_t)tag_id);
                    //tag = tags.GetValueOrDefault(tag_id);
                    bool b = tags.TryGetValue(tag_id, out tag);
                    if (!b) tag = null;

                    if (tag != null)
                    {
                        //debug_set_tag_id(tag->tag_id);
                    }
                    else
                    {
                        /* TODO - remove this. */
                        //pdebug(DEBUG_WARN, "Tag with ID %d not found.", tag_id);
                    }

                    if ((tag !=null) && tag.tag_id == tag_id)
                    {
                        //pdebug(DEBUG_SPEW, "Found tag %p with id %d.", tag, tag->tag_id);
                        //tag = rc_inc(tag);
                    }
                    else
                    {
                        //debug_set_tag_id(0);
                        tag = null;
                    }
                }
                tag_lookup_mutex.mutex_unlock();

                return tag;
            }

        /*
         * plc_tag_abort()  
         *
         * This function calls through the vtable in the passed tag to call
         * the protocol-specific implementation.
         *
         * The implementation must do whatever is necessary to abort any
         * ongoing IO.
         *
         * The status of the operation is returned.
         */

        public static int plc_tag_abort(Int32 id)
        {
            int rc = PLCTAG_STATUS_OK;
            PlcTag tag = lookup_tag(id);

            //pdebug(DEBUG_INFO, "Starting.");

            if (tag==null)
            {
                //pdebug(DEBUG_WARN, "Tag not found.");
                return PLCTAG_ERR_NOT_FOUND;
            }

            tag.api_mutex.mutex_lock();
            do
            {
                //critical_block(tag->api_mutex) {
                /* who knows what state the tag data is in.  */
                tag.read_cache_expire = 0;

                /* this may be synchronous. */
                //if (tag->vtable && tag->vtable->abort)
                //{
                //    rc = tag->vtable->abort(tag);
                //}
                //else
                //{
                //    pdebug(DEBUG_WARN, "Tag does not have an abort function.");
                //    rc = PLCTAG_ERR_NOT_IMPLEMENTED;
                //}
                tag.abort();
                tag.read_in_flight = false;
                tag.read_complete = false;
                tag.write_in_flight = false;
                tag.write_complete = false;

                //tag_raise_event(tag, PLCTAG_EVENT_ABORTED, PLCTAG_ERR_ABORT);
            } while (false);
            tag.api_mutex.mutex_unlock();


            /* release the kraken... or tickler */
            plc_tag_tickler_wake();

            tag.plc_tag_generic_handle_event_callbacks();

            //rc_dec(tag);

            //pdebug(DEBUG_INFO, "Done.");

            return rc;
        }

        /*
         * plc_tag_status
         *
         * Return the current status of the tag.  This will be PLCTAG_STATUS_PENDING if there is
         * an uncompleted IO operation.  It will be PLCTAG_STATUS_OK if everything is fine.  Other
         * errors will be returned as appropriate.
         *
         * This is a function provided by the underlying protocol implementation.
         */

        public static int plc_tag_status(Int32 id)
        {
            int rc = PLCTAG_STATUS_OK;
            PlcTag tag = lookup_tag(id);

            //pdebug(DEBUG_SPEW, "Starting.");

            /* check the ID.  It might be an error status from creating the tag. */
            if (tag==null)
            {
                if (id < 0)
                {
                    //pdebug(DEBUG_WARN, "Called with an error status %s!", plc_tag_decode_error(id));
                    return id;
                }
                else
                {
                    //pdebug(DEBUG_WARN, "Tag not found.");
                    return PLCTAG_ERR_NOT_FOUND;
                }
            }

            tag.api_mutex.mutex_lock();
            do {
                //critical_block(tag->api_mutex) {
                /*if (tag.vtable && tag->vtable->tickler)
                {
                    tag->vtable->tickler(tag);
                }*/
                tag.tickler();

                /*if (tag->vtable && tag->vtable->status)
                {
                    rc = tag->vtable->status(tag);
                }
                else
                {
                    rc = PLCTAG_ERR_NOT_IMPLEMENTED;
                }*/
                rc = tag.status();

                if (rc == PLCTAG_STATUS_OK)
                {
                    if (tag.read_in_flight || tag.write_in_flight)
                    {
                        rc = PLCTAG_STATUS_PENDING;
                    }
                }
            } while (false);
            tag.api_mutex.mutex_unlock();

            //rc_dec(tag);

            //pdebug(DEBUG_SPEW, "Done with rc=%s.", plc_tag_decode_error(rc));

            return rc;
        }


        public static Int32 plc_tag_get_int32(Int32 id, int offset)
        {
            Int32 res = Int32.MinValue; // INT32_MIN;
            PlcTag tag = lookup_tag(id);

            //pdebug(DEBUG_SPEW, "Starting.");

            if (tag == null)
            {
                //pdebug(DEBUG_WARN, "Tag not found.");
                return res;
            }

            /* is there data? */
            if (tag.data== null)
            {
                //pdebug(DEBUG_WARN, "Tag has no data!");
                tag.status_ = PLCTAG_ERR_NO_DATA;
                //rc_dec(tag);
                return res;
            }

            if (!tag.is_bit)
            {
                tag.api_mutex.mutex_lock();
                /*critical_block(tag->api_mutex)*/
                {
                    if ((offset >= 0) && (offset + (/*(int)sizeof(int32_t)*/4) <= tag.size))
                    {
                        res = (Int32)(((UInt32)(tag.data[offset + tag.byte_order.int32_order[0]]) << 0) +
                                        ((Int32)(tag.data[offset + tag.byte_order.int32_order[1]]) << 8) +
                                        ((Int32)(tag.data[offset + tag.byte_order.int32_order[2]]) << 16) +
                                        ((UInt32)(tag.data[offset + tag.byte_order.int32_order[3]]) << 24));

                        tag.status_ = PLCTAG_STATUS_OK;
                    }
                    else
                    {
                        //pdebug(DEBUG_WARN, "Data offset out of bounds!");
                        tag.status_ = PLCTAG_ERR_OUT_OF_BOUNDS;
                    }
                } while (false) ;
                tag.api_mutex.mutex_unlock();
            }
            else
            {
                int rc = plc_tag_get_bit(id, tag.bit);

                /* make sure the response is good. */
                if (rc >= 0)
                {
                    res = (Int32)rc;
                }
            }

            //rc_dec(tag);

            return res;
        }

        public static int plc_tag_get_bit(Int32 id, int offset_bit)
        {
            int res = PLCTAG_ERR_OUT_OF_BOUNDS;
            int real_offset = offset_bit;
            PlcTag tag = lookup_tag(id);

            //pdebug(DEBUG_SPEW, "Starting.");

            if (tag==null)
            {
                //pdebug(DEBUG_WARN, "Tag not found.");
                return PLCTAG_ERR_NOT_FOUND;
            }

            /* is there data? */
            if (tag.data==null)
            {
                //pdebug(DEBUG_WARN, "Tag has no data!");
                tag.status_ = PLCTAG_ERR_NO_DATA;
                //rc_dec(tag);
                return PLCTAG_ERR_NO_DATA;
            }

            /* if this is a single bit, then make sure the offset is the tag bit. */
            if (tag.is_bit)
            {
                real_offset = tag.bit;
            }
            else
            {
                real_offset = offset_bit;
            }

            //pdebug(DEBUG_SPEW, "selecting bit %d with offset %d in byte %d (%x).", real_offset, (real_offset % 8), (real_offset / 8), tag->data[real_offset / 8]);

            tag.api_mutex.mutex_lock();
            do /*
            critical_block(tag->api_mutex) */
            {
                if ((real_offset >= 0) && ((real_offset / 8) < tag.size))
                {
                    res = ~/*!!*/(((1 << (real_offset % 8)) & 0xFF) & (tag.data[real_offset / 8]));
                    tag.status_ = PLCTAG_STATUS_OK;
                }
                else
                {
                    //pdebug(DEBUG_WARN, "Data offset out of bounds!");
                    res = PLCTAG_ERR_OUT_OF_BOUNDS;
                    tag.status_ = PLCTAG_ERR_OUT_OF_BOUNDS;
                }
            } while (false);
            tag.api_mutex.mutex_unlock();

            //rc_dec(tag);

            return res;
        }

    }
}