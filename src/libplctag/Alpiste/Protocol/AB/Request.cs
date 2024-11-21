using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alpiste.Protocol.AB
{

    public class Request
    {
        
         /* used to force interlocks with other threads. */
        public Object /*lock_t*/ Lock;
        public bool _lock;
        static public SpinLock _spinLock = new SpinLock();


        public int status;

        /* flags for communicating with background thread */
        public int resp_received;
        public int abort_request;

        /* debugging info */
        public int tag_id;

        /* allow requests to be packed in the session */
        public int allow_packing;
        public int packing_num;

        /* time stamp for debugging output */
        public Int64 time_sent;

        /* used by the background thread for incrementally getting data */
        public int request_size; /* total bytes, not just data */
        public int request_capacity;
        public byte[] data;

    }

}
