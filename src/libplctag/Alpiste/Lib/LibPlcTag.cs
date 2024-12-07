using System;

namespace LibPlcTag_
{
    public class Status
    {
        /* library internal status. */
        public const int PLCTAG_STATUS_PENDING = 1;
        public const int PLCTAG_STATUS_OK = 0;

        public const int PLCTAG_ERR_ABORT = -1;
        public const int PLCTAG_ERR_BAD_CONFIG = -2;
        public const int PLCTAG_ERR_BAD_CONNECTION = -3;
        public const int PLCTAG_ERR_BAD_DATA = -4;
        public const int PLCTAG_ERR_BAD_DEVICE = -5;
        public const int PLCTAG_ERR_BAD_GATEWAY = -6;
        public const int PLCTAG_ERR_BAD_PARAM = -7;
        public const int PLCTAG_ERR_BAD_REPLY = -8;
        public const int PLCTAG_ERR_BAD_STATUS = -9;
        public const int PLCTAG_ERR_CLOSE = -10;
        public const int PLCTAG_ERR_CREATE = -11;
        public const int PLCTAG_ERR_DUPLICATE = -12;
        public const int PLCTAG_ERR_ENCODE = -13;
        public const int PLCTAG_ERR_MUTEX_DESTROY = -14;
        public const int PLCTAG_ERR_MUTEX_INIT = -15;
        public const int PLCTAG_ERR_MUTEX_LOCK = -16;
        public const int PLCTAG_ERR_MUTEX_UNLOCK = -17;
        public const int PLCTAG_ERR_NOT_ALLOWED = -18;
        public const int PLCTAG_ERR_NOT_FOUND = -19;
        public const int PLCTAG_ERR_NOT_IMPLEMENTED = -20;
        public const int PLCTAG_ERR_NO_DATA = -21;
        public const int PLCTAG_ERR_NO_MATCH = -22;
        public const int PLCTAG_ERR_NO_MEM = -23;
        public const int PLCTAG_ERR_NO_RESOURCES = -24;
        public const int PLCTAG_ERR_NULL_PTR = -25;
        public const int PLCTAG_ERR_OPEN = -26;
        public const int PLCTAG_ERR_OUT_OF_BOUNDS = -27;
        public const int PLCTAG_ERR_READ = -28;
        public const int PLCTAG_ERR_REMOTE_ERR = -29;
        public const int PLCTAG_ERR_THREAD_CREATE = -30;
        public const int PLCTAG_ERR_THREAD_JOIN = -31;
        public const int PLCTAG_ERR_TIMEOUT = -32;
        public const int PLCTAG_ERR_TOO_LARGE = -33;
        public const int PLCTAG_ERR_TOO_SMALL = -34;
        public const int PLCTAG_ERR_UNSUPPORTED = -35;
        public const int PLCTAG_ERR_WINSOCK = -36;
        public const int PLCTAG_ERR_WRITE = -37;
        public const int PLCTAG_ERR_PARTIAL = -38;
        public const int PLCTAG_ERR_BUSY = -39;

        ~Status()
        {
            Console.WriteLine("Destruyendo Status");
        }
    }


}
    