using Alpiste.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Alpiste.Lib.PlcTag;
using System.Xml.Linq;
using Alpiste.Utils;
using System.Numerics;
using Alpiste.Protocol.AB;
using System.Reflection;
using System.Data;
using static libplctag.NativeImport.plctag;

namespace Alpiste.Protocol.System_
{

    public class SystemTag:PlcTag
    {
        const int MAX_SYSTEM_TAG_SIZE = 30;
        String name;
        byte[] backing_data = new byte[MAX_SYSTEM_TAG_SIZE];

        public class system_tag_byte_order: tag_byte_order_t
        {
            //char name[MAX_SYSTEM_TAG_NAME];
            public system_tag_byte_order()
            {
                is_allocated = false;
                int16_order = new Int16[] { 0, 1 };
                int32_order = new Int16[] { 0, 1, 2, 3 };
                int64_order = new Int16[] { 0, 1, 2, 3, 4, 5, 6, 7 };
                float32_order = new Int16[] { 0, 1, 2, 3 };
                float64_order = new Int16[] { 0, 1, 2, 3, 4, 5, 6, 7 };
                str_is_defined = true;
                str_is_counted = false;
                str_is_fixed_length = false;
                str_is_zero_terminated = true; /* C-style string. */
                str_is_byte_swapped = false;
                str_pad_to_multiple_bytes = 1;
                str_count_word_bytes = 0;
                str_max_capacity = 0;
                str_total_length = 0;
                str_pad_bytes = 0;
            }
        }
  
        SystemTag(attr attribs, callback_func_ex tag_callback_func, Object userdata): base(attribs, tag_callback_func, userdata)  
        {

            String name = Attr.attr_get_str(attribs, "name", null);
            /* check the name, if none given, punt. */
            if (name == null || name.Length < 1)
            {
                throw new NoNameException();
            }

            //plc_tag_generic_init_tag(attribs, tag_callback_func, userdata);

            /* set the byte order. */
            byte_order = new system_tag_byte_order();

            /* get the name and copy it */
            this.name = name;
            
            /* point data at the backing store. */
            data = backing_data;//&tag->backing_data[0];
            size = backing_data.Length; // (int)sizeof(tag->backing_data);

            //pdebug(DEBUG_INFO, "Done");
        }

        public static PlcTag system_tag_create(attr attribs, callback_func_ex /*TagCallbackFunc*/ tag_callback_func, Object userdata)
        {
            return new SystemTag(attribs, tag_callback_func, userdata);
        }

        override public int abort() {
            /* there are no outstanding operations, so everything is OK. */
            status_ = PLCTAG_STATUS_OK;
            return PLCTAG_STATUS_OK;
        }

        public override int read()
        {
            int rc = PLCTAG_STATUS_OK;

            //pdebug(DEBUG_INFO, "Starting.");

            
            if ("version".CompareTo(name)==0)
            {
                //pdebug(DEBUG_DETAIL, "Version is %s", VERSION);
                string version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
                data = new byte[version.Length + 1];
                for (int i = 0; i<version.Length; i++)
                {
                    data[i] = (byte) version[i];
                }
                data[version.Length] = 0;
                value = version;
                rc = PLCTAG_STATUS_OK;
            }
            /*else if (str_cmp_i(&tag->name[0], "debug") == 0)
            {
                int debug_level = get_debug_level();
                tag->data[0] = (uint8_t)(debug_level & 0xFF);
                tag->data[1] = (uint8_t)((debug_level >> 8) & 0xFF);
                tag->data[2] = (uint8_t)((debug_level >> 16) & 0xFF);
                tag->data[3] = (uint8_t)((debug_level >> 24) & 0xFF);
                rc = PLCTAG_STATUS_OK;
            }*/
            else
            {
                //pdebug(DEBUG_WARN, "Unsupported system tag %s!", tag->name);
                rc = PLCTAG_ERR_UNSUPPORTED;
            }

            /* safe here because we are still within the API mutex. */
            //tag_raise_event((plc_tag_p)tag, PLCTAG_EVENT_READ_STARTED, PLCTAG_STATUS_OK);
            event_read_started = true;
            event_read_started_status = (byte)status_;
            event_read_complete = true;
            event_read_complete_status = (byte)status_;

            //tag_raise_event((plc_tag_p)tag, PLCTAG_EVENT_READ_COMPLETED, PLCTAG_STATUS_OK);
            plc_tag_generic_handle_event_callbacks();

            //pdebug(DEBUG_INFO, "Done.");

            return rc;
        }

        public override int status()
        {
            status_ = PLCTAG_STATUS_OK;
            return PLCTAG_STATUS_OK;
        }

        public override int write()
        {
            int rc = PLCTAG_STATUS_OK;
            /*system_tag_p tag = (system_tag_p)ptag;

            if (!tag)
            {
                return PLCTAG_ERR_NULL_PTR;
            }

            /* raise this here so that the callback can update the tag buffer. */
            /*tag_raise_event((plc_tag_p)tag, PLCTAG_EVENT_WRITE_STARTED, PLCTAG_STATUS_PENDING);
            plc_tag_generic_handle_event_callbacks((plc_tag_p)tag);

            /* the version is static */
            /*if (str_cmp_i(&tag->name[0], "debug") == 0)
            {
                int res = 0;
                res = (int32_t)(((uint32_t)(tag->data[0])) +
                                ((uint32_t)(tag->data[1]) << 8) +
                                ((uint32_t)(tag->data[2]) << 16) +
                                ((uint32_t)(tag->data[3]) << 24));
                set_debug_level(res);
                rc = PLCTAG_STATUS_OK;
            }
            else if (str_cmp_i(&tag->name[0], "version") == 0)
            {
                rc = PLCTAG_ERR_NOT_IMPLEMENTED;
            }
            else
            {
                pdebug(DEBUG_WARN, "Unsupported system tag %s!", tag->name);*/
                rc = PLCTAG_ERR_UNSUPPORTED;
            /*}*/

            //tag_raise_event((plc_tag_p)tag, PLCTAG_EVENT_WRITE_COMPLETED, PLCTAG_STATUS_OK);
            event_write_complete = true;
            event_write_complete_status = (byte)status_;

            plc_tag_generic_handle_event_callbacks();

            //pdebug(DEBUG_INFO, "Done.");

            return rc;
        }
       
    }
}
