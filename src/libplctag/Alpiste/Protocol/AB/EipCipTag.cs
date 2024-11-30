using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static libplctag.NativeImport.plctag;

namespace Alpiste.Protocol.AB
{
    internal class EipCipTag:AbTag
    {
        /* define the exported vtable for this tag type. */
        /*  struct tag_vtable_t eip_cip_vtable = {
              (tag_vtable_func) ab_tag_abort, /* shared */
        /*      (tag_vtable_func)tag_read_start,
              (tag_vtable_func) ab_tag_status, /* shared */
        /*      (tag_vtable_func)tag_tickler,
              (tag_vtable_func) tag_write_start,
              (tag_vtable_func)NULL, /* wake_plc */

        /* attribute accessors */
        //     ab_get_int_attrib,
        //     ab_set_int_attrib,

        //     ab_get_byte_array_attrib
        //       };*/

        /*override public int abort() { return 0; }
        override public int read() { return tag_read_start(); }
        override public int get_status() { return ab_tag_status(); }
        override public int tickler() { return tag_tickler(); }
        override public int write() { return 0; }

        override public int wake_plc() { return 0; }
        */

        public EipCipTag(Utils.attr attribs, callback_func_ex tag_callback_func, Object userdata) : base(attribs, tag_callback_func, userdata)
        {

        }

 

        

    }
}
