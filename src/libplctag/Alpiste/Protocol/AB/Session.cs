using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alpiste.Utils;
using Alpiste.Protocol.AB;
using static Alpiste.Lib.PlcTag;
using Alpiste.Lib;
using System.Net.Sockets;
using System.Threading;
using static System.Collections.Specialized.BitVector32;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Net.Cache;
using System.Net;
using System.Linq.Expressions;


namespace Alpiste.Protocol.AB
{

    public  class Session:IDisposable
    {
        const int MAX_REQUESTS = 200;

        const int EIP_CIP_PREFIX_SIZE = 44; /* bytes of encap header and CFP connected header */

        const int MAX_CIP_LGX_MSG_SIZE = (0x01FF & 504);
        const int MAX_CIP_LGX_MSG_SIZE_EX = (0xFFFF & 4002);

        const int MAX_CIP_MICRO800_MSG_SIZE = (0x01FF & 504);
        const int MAX_CIP_MICRO800_MSG_SIZE_EX = (0xFFFF & 4002);

        /* Omron is special */
        // #define MAX_CIP_OMRON_MSG_SIZE_EX (0xFFFF & 1994)
        // #define MAX_CIP_OMRON_MSG_SIZE (0x01FF & 502)

        /* maximum for PCCC embedded within CIP. */
        const int MAX_CIP_PLC5_MSG_SIZE = 244;
        // #define MAX_CIP_SLC_MSG_SIZE (222)
        const int MAX_CIP_SLC_MSG_SIZE = 244;
        const int MAX_CIP_MLGX_MSG_SIZE = 244;
        const int MAX_CIP_LGX_PCCC_MSG_SIZE = 244;

        /*
         * Number of milliseconds to wait to try to set up the session again
         * after a failure.
         */
        const int RETRY_WAIT_MS = 5000;

        const int SESSION_DISCONNECT_TIMEOUT = 5000;

        const int SOCKET_WAIT_TIMEOUT_MS = 20;
        const int SESSION_IDLE_WAIT_TIME = 100;

        /* make sure we try hard to get a good payload size */
        // #define GET_MAX_PAYLOAD_SIZE(sess) ((sess->max_payload_size > 0) ? (sess->max_payload_size) : ((sess->fo_conn_size > 0) ? (sess->fo_conn_size) : (sess->fo_ex_conn_size)))



        /* #define MAX_SESSION_HOST    (128) */

        public const int SESSION_DEFAULT_TIMEOUT = 2000;

        public const int MAX_PACKET_SIZE_EX = (44 + 4002) ;

        public const int SESSION_MIN_REQUESTS = (10);
        public const int SESSION_INC_REQUESTS = (10);

        public const int MAX_CONN_PATH = (260);   /* 256 plus padding. */
        public const int MAX_IP_ADDR_SEG_LEN = (16);




        public int failed;
        public int on_list;

        /* gateway connection related info */
        public String host;
        public int port;
        public String path;
        public Socket sock;

        /* connection variables. */
        public bool use_connected_msg;
        public bool only_use_old_forward_open;
        public int fo_conn_size; /* old FO max connection size */
        public int fo_ex_conn_size; /* extended FO max connection size */
        public UInt16 max_payload_guess;
        public UInt16 max_payload_size;

        public UInt32 orig_connection_id;
        public UInt32 targ_connection_id;
        public UInt16 conn_seq_num;
        public UInt16 conn_serial_number;

        public PlcType plc_type;

        public byte[] conn_path;
        public byte conn_path_size;
        public UInt16 dhp_dest;
        public int is_dhp;

        public int connection_group_id;

        /* registration info */
        public UInt32 session_handle;

        /* Sequence ID for requests. */
        public UInt64 session_seq_id;

        /* list of outstanding requests for this session */
        HashSet<Request> /*vector_p*/ requests;

        public UInt64 resp_seq_id;

        /* data for receiving messages */
        public UInt32 data_offset;
        public UInt32 data_capacity;
        public UInt32 data_size;
        public byte[] data;
        public bool data_buffer_is_static;
        // uint8_t data[MAX_PACKET_SIZE_EX];

        public UInt64 packet_count;

        Thread /*thread_p*/ handler_thread;
        public volatile int terminating;
        mutex_t /*mutex_p*/ mutex;
        
        Cond /*  cond_p*/ wait_cond;

        /* disconnect handling */
        public int auto_disconnect_enabled;
        public int auto_disconnect_timeout_ms;


        public static mutex_t session_mutex = new mutex_t();
        public static HashSet<Session> sessions = new HashSet<Session>();
        public HashSet<PlcTag> tags_references = new HashSet<PlcTag>();


        public static Random rand = new Random();
        private bool disposedValue;

        static public Session session_find_or_create(/*ref Session tag_session,*/ attr attribs)
        {
            /*int debug = attr_get_int(attribs,"debug",0);*/
            String session_gw = Attr.attr_get_str(attribs, "gateway", "");
            String session_path = Attr.attr_get_str(attribs, "path", "");
            int use_connected_msg = Attr.attr_get_int(attribs, "use_connected_msg", 0);
            //int session_gw_port = attr_get_int(attribs, "gateway_port", AB_EIP_DEFAULT_PORT);
            PlcType plc_type = AbTag.get_plc_type(attribs);
            Session session = null; // AB_SESSION_NULL;
            int new_session = 0;
            int shared_session = Attr.attr_get_int(attribs, "share_session", 1); /* share the session by default. */
            int rc = PlcTag.PLCTAG_STATUS_OK;
            int auto_disconnect_enabled = 0;
            int auto_disconnect_timeout_ms = Int16.MaxValue; //  INT_MAX;
            int connection_group_id = Attr.attr_get_int(attribs, "connection_group_id", 0);
            int only_use_old_forward_open = Attr.attr_get_int(attribs, "conn_only_use_old_forward_open", 0);

            //pdebug(DEBUG_DETAIL, "Starting");

            auto_disconnect_timeout_ms = Attr.attr_get_int(attribs, "auto_disconnect_ms", Int16.MaxValue /* INT_MAX*/);
            if (auto_disconnect_timeout_ms != Int16.MaxValue) // INT_MAX)
            {
                //pdebug(DEBUG_DETAIL, "Setting auto-disconnect after %dms.", auto_disconnect_timeout_ms);
                auto_disconnect_enabled = 1;
            }

            // if(plc_type == AB_PLC_PLC5 && str_length(session_path) > 0) {
            //     /* this means it is DH+ */
            //     use_connected_msg = 1;
            //     attr_set_int(attribs, "use_connected_msg", 1);
            // }

            //critical_block(session_mutex) {
            
            lock (session_mutex) { 
                /* if we are to share sessions, then look for an existing one. */
                if (shared_session !=0)
                {
                    session = find_session_by_host_unsafe(session_gw, session_path, connection_group_id);
                }
                else
                {
                /* no sharing, create a new one */
                session = null; // AB_SESSION_NULL;
                }

                if (session == null /*AB_SESSION_NULL*/)
                {
                    //pdebug(DEBUG_DETAIL, "Creating new session.");

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
                            session = create_lgx_session_unsafe(session_gw, session_path, ref use_connected_msg, connection_group_id);
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

                    if (session == null /*AB_SESSION_NULL*/)
                    {
                        //pdebug(DEBUG_WARN, "unable to create or find a session!");
                        rc = PlcTag.PLCTAG_ERR_BAD_GATEWAY;
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
                    if (!(session.auto_disconnect_enabled !=0) && (auto_disconnect_enabled !=0))
                    {
                        session.auto_disconnect_enabled = auto_disconnect_enabled;
                    }

                    /* disconnect period always goes down. */
                    if (session.auto_disconnect_enabled !=0 && session.auto_disconnect_timeout_ms > auto_disconnect_timeout_ms)
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

            if (new_session != null)
            {
                rc = session.session_init(/*session*/);
                if (rc != PlcTag.PLCTAG_STATUS_OK)
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

            /* store it into the tag */
            //tag_session = session;
            return session;
            //pdebug(DEBUG_DETAIL, "Done");

            //return rc;
        }

        /*
         * session_init
         *
         * This calls several blocking methods and so must not keep the main mutex
         * locked during them.
         */
        int session_init(/*ab_session_p session*/)
        {
            int rc = PlcTag.PLCTAG_STATUS_OK;

            //pdebug(DEBUG_INFO, "Starting.");

            /* create the session mutex. */
            mutex = new mutex_t();
            //if ((rc = mutex_create(&(session->mutex))) != PLCTAG_STATUS_OK)
            //{
            //    pdebug(DEBUG_WARN, "Unable to create session mutex!");
            //    session.failed = 1;
            //    return rc;
            //}

            /* create the session condition variable. */
            wait_cond = new Cond();
            //           if ((rc = cond_create(&(session->wait_cond))) != PLCTAG_STATUS_OK)
            //           {
            //               pdebug(DEBUG_WARN, "Unable to create session condition var!");
            //               session->failed = 1;
            //               return rc;
            //           }

            handler_thread = new Thread(new System.Threading.ParameterizedThreadStart(session_handler));
            handler_thread.Start(this);
            handler_thread = null;
            /*if ((rc = thread_create((thread_p*)&(session->handler_thread), session_handler, 32 * 1024, session)) != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_WARN, "Unable to create session thread!");
                session.failed = 1;
                return rc;
            }*/

            //pdebug(DEBUG_INFO, "Done.");

            return rc;
        }


        static Session find_session_by_host_unsafe(String host, String path, int connection_group_id)
        {
            foreach (Session session in sessions)
            
            //for (int i = 0; i < vector_length(sessions); i++)
            {
                //ab_session_p session = vector_get(sessions, i);

                /* is this session in the process of destruction? */
                //session = rc_inc(session);
                //if (session)
                //{
                    if (session.connection_group_id == connection_group_id && session.session_match_valid(host, path /*, session*/) != 0)
                    {
                        return session;
                    }

                //    rc_dec(session);
                //}
            }

            return null; // NULL;
        }

        public int session_match_valid(String host, String path /*, ab_session_p session*/)
        {       
            /*if (!session)
            {
                return 0;
            }*/

            /* don't use sessions that failed immediately. */
                if (/*session->*/failed !=0)
                {
                    return 0;
                }

            if (host.Length == 0)
            {
                //pdebug(DEBUG_WARN, "New session host is NULL or zero length!");
                return 0;
            }

            if (/*session->*/host.Length == 0)
            {
                //pdebug(DEBUG_WARN, "Session host is NULL or zero length!");
                return 0;
            }

            if (host.ToLower() != /*session->*/host.ToLower())
            {
                return 0;
            }

            if (path.ToLower() != /*session->*/path.ToLower())
            {
                return 0;
            }

            return 1;
        }

        static Session create_lgx_session_unsafe(String host, String path, ref int use_connected_msg, int connection_group_id)
        {
            Session session = null;

            //pdebug(DEBUG_INFO, "Starting.");

            //do {
                session = session_create_unsafe(MAX_CIP_LGX_MSG_SIZE_EX, true, host, path, PlcType.AB_PLC_LGX, ref use_connected_msg, connection_group_id);
                if(session != null) {
                    session.only_use_old_forward_open = false;
                    session.fo_conn_size = MAX_CIP_LGX_MSG_SIZE;
                    session.fo_ex_conn_size = MAX_CIP_LGX_MSG_SIZE_EX;
                    session.max_payload_size = (UInt16) session.fo_conn_size;
                } else {
                    //pdebug(DEBUG_WARN, "Unable to create *Logix session!");
                }
            //} while (0) ;

            //pdebug(DEBUG_INFO, "Done.");

            return session;
        }


        static Session session_create_unsafe(int max_payload_capacity, bool data_buffer_is_static, String host, String path, PlcType plc_type, ref int use_connected_msg, int connection_group_id)
        {
            /*static volatile*/ UInt32 connection_id = 0;

            int rc = PlcTag.PLCTAG_STATUS_OK;
            Session session = null; // AB_SESSION_NULL;
 //*HR*     //int total_allocation_size = sizeof(*session);
 /*HR*/     int total_allocation_size = 0;

            int data_buffer_capacity = EIP_CIP_PREFIX_SIZE + max_payload_capacity;
            int data_buffer_offset = 0;
            int host_name_offset = 0;
            int host_name_size = 0;
            int path_offset = 0;
            int path_size = 0;
            int conn_path_offset = 0;
            byte[] tmp_conn_path = new byte[MAX_CONN_PATH + MAX_IP_ADDR_SEG_LEN];
            int tmp_conn_path_size = MAX_CONN_PATH + MAX_IP_ADDR_SEG_LEN;
            int is_dhp = 0;
            UInt16 dhp_dest = 0;

            //pdebug(DEBUG_INFO, "Starting");

            if(use_connected_msg == 1) {
                //pdebug(DEBUG_DETAIL, "Session should use connected messaging.");
            } else {
                //pdebug(DEBUG_DETAIL, "Session should not use connected messaging.");
            }

            /* add in space for the data buffer. */
        /*    if (data_buffer_is_static)
            {
                data_buffer_offset = total_allocation_size;
                total_allocation_size += data_buffer_capacity;
            }
            else */
            {
                data_buffer_offset = 0;
            }

            /* add in space for the host name.  + 1 for the NUL terminator. */
            host_name_offset = total_allocation_size;
            host_name_size = host.Length + 1;
            total_allocation_size += host_name_size;

            /* add in space for the path copy. */
            if (path != null && path.Length > 0)
            {
                path_offset = total_allocation_size;
                path_size = path.Length + 1;
                total_allocation_size += path_size;
            }
            else
            {
                path_offset = 0;
            }

            /* encode the path */
            rc = CIP.cip_encode_path(path, ref use_connected_msg, plc_type, ref tmp_conn_path, ref tmp_conn_path_size, ref is_dhp, ref dhp_dest);
            if (rc != PlcTag.PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_INFO, "Unable to convert path string to binary path, error %s!", plc_tag_decode_error(rc));
                // rc_dec(session);
                return null;
            }

            conn_path_offset = total_allocation_size;
            total_allocation_size += tmp_conn_path_size;

            /* allocate the session struct and the buffer in the same allocation. */
            //pdebug(DEBUG_DETAIL, "Allocating %d total bytes of memory with %d bytes for data buffer static data, %d bytes for the host name, %d bytes for the path, %d bytes for the encoded path.",
            //          total_allocation_size,
            //          (data_buffer_is_static ? data_buffer_capacity : 0),
            //          str_length(host) + 1,
            //          (path_offset == 0 ? 0 : str_length(path) + 1),
            //          tmp_conn_path_size
            //          );

            /*session = (ab_session_p)rc_alloc(total_allocation_size, session_destroy);
            if (!session)
            {
                pdebug(DEBUG_WARN, "Error allocating new session!");
                return AB_SESSION_NULL;
            }*/

            session = new Session();
            
            /* fill in the interior pointers */

            /* fix up the data buffer. */
            session.data_buffer_is_static = data_buffer_is_static;
            session.data_capacity = (UInt32) data_buffer_capacity;

            if (data_buffer_is_static)
            {
                //session.data = (uint8_t*)(session) + data_buffer_offset;
                // session->data_capacity = max_buffer_size;
                session.data = new byte[total_allocation_size];
            }
            else
            {
                //session->data = (uint8_t*)mem_alloc(data_buffer_capacity);
                session.data = new byte[data_buffer_capacity];

                /*if (session->data == NULL)
                {
                    pdebug(DEBUG_WARN, "Unable to allocate the connection data buffer!");
                    return rc_dec(session);
                }*/
            }

            /* point the host pointer just after the data. */
            //session->host = (char*)(session) + host_name_offset;
            session.host = host;
            //str_copy(session->host, host_name_size, host);
            for (int i = 0; i<host_name_size -1 ; i++)
            {
                session.data[host_name_offset + i] = (byte) host[i];
            }
            session.data[host_name_offset + host_name_size -1] = 0;

            if (path_offset!=0)
            {
                //session->path = (char*)(session) + path_offset;
                session.path = path;
                //str_copy(session->path, path_size, path);
                for (int i = 0;i<path_size -1 ; i++)
                {
                    session.data[path_offset + i] = (byte) path[i];
                }
                session.data[path_offset + path_size -1] = 0;   
            }

            if (conn_path_offset!=0)
            {
                //session.conn_path = (uint8_t*)(session) + conn_path_offset;
                session.conn_path = tmp_conn_path;
                session.conn_path_size = (byte) tmp_conn_path_size;

                // FIXME - the path length cannot be 8 bits with a buffer length that is over 260.
                //session->conn_path_size = (uint8_t)tmp_conn_path_size;
                //mem_copy(session->conn_path, tmp_conn_path, tmp_conn_path_size);
                for (int i = 0; i<tmp_conn_path_size; i++)
                {
                    session.data[conn_path_offset+ i ] = tmp_conn_path[i];
                }
            }


            /*
                TO DO
                    remove mem_free from destructor for host, path, and conn_path.
            */


            //session->requests = vector_create(SESSION_MIN_REQUESTS, SESSION_INC_REQUESTS);
            //if (!session->requests)
            //{
            //    pdebug(DEBUG_WARN, "Unable to allocate vector for requests!");
            //    rc_dec(session);
            //    return NULL;
            //}
            session.requests = new HashSet<Request> ();


            /* check for ID set up. This does not need to be thread safe since we just need a random value. */
            if (connection_id == 0)
            {
                connection_id = (UInt32)rand.Next();
            }

            /* fix up the rest of teh fields */
            session.plc_type = plc_type;
            session.use_connected_msg = use_connected_msg != 0;
            session.failed = 0;
            session.conn_serial_number = (UInt16)(rand.Next() & 0xFFFF);
            session.session_seq_id = (UInt64)rand.Next();
            session.is_dhp = is_dhp;
            session.dhp_dest = dhp_dest;

            //pdebug(DEBUG_DETAIL, "Setting connection_group_id to %d.", connection_group_id);
            session.connection_group_id = connection_group_id;

            /*
             * Why is connection_id global?  Because it looks like the PLC might
             * be treating it globally.  I am seeing ForwardOpen errors that seem
             * to be because of duplicate connection IDs even though the session
             * was closed.
             *
             * So, this is more or less unique across all invocations of the library.
             * FIXME - this could collide.  The probability is low, but it could happen
             * as there are only 32 bits.
             */

            session.orig_connection_id = ++connection_id;

            /* add the new session to the list. */
            //session.add_session_unsafe();
            sessions.Add(session);
            session.on_list = 1;

            //pdebug(DEBUG_INFO, "Done");

            return session;
        }

  /*      public int add_session_unsafe(/*ab_session_p session*//*)
        {
            //pdebug(DEBUG_DETAIL, "Starting");

            /*if (!session)
            {
                return PLCTAG_ERR_NULL_PTR;
            }*/

/*            sessions.Add(this);

            //vector_put(sessions, vector_length(sessions), session);

            on_list = 1;

            //pdebug(DEBUG_DETAIL, "Done");

            return PlcTag.PLCTAG_STATUS_OK;
        }
*/

        public int session_create_request(int tag_id, ref Request req)
        {
            int rc = PlcTag.PLCTAG_STATUS_OK;
            Request res;
            int /*size_t*/ request_capacity = 0;
            byte[] buffer = null;

            //critical_block(session->mutex) {
            int max_payload_size = GET_MAX_PAYLOAD_SIZE();

            // FIXME: no logging in a mutex!
            // pdebug(DEBUG_DETAIL, "FIXME: max payload size %d", max_payload_size);

            request_capacity = (int /*size_t*/)(max_payload_size + EIP_CIP_PREFIX_SIZE);
            //}

            //pdebug(DEBUG_DETAIL, "Starting.");

            //buffer = (uint8_t*)mem_alloc((int)request_capacity);
            buffer = new byte[request_capacity];

            /*if (!buffer)
            {
                pdebug(DEBUG_WARN, "Unable to allocate request buffer!");
                *req = NULL;
                return PLCTAG_ERR_NO_MEM;
            }*/

            //res = (ab_request_p)rc_alloc((int)sizeof(struct ab_request_t), request_destroy);
            res = new Request();

            /*if (!res) {
                mem_free(buffer);
                *req = NULL;
                rc = PLCTAG_ERR_NO_MEM;
            } else {*/
            res.data = buffer;
            res.tag_id = tag_id;
            res.request_capacity = (int)request_capacity;
            res.Lock = 0; // LOCK_INIT;

            req = res;
        //}

        //pdebug(DEBUG_DETAIL, "Done.");

        return rc;
        }

        int GET_MAX_PAYLOAD_SIZE() {
            if (max_payload_size > 0) {
                return max_payload_size;
            }
            else
            {
                if (fo_conn_size > 0)
                {
                    return fo_conn_size;
                } else
                {
                    return fo_ex_conn_size;
                }

            }
        }

        /*
         * session_add_request
         *
         * This is a thread-safe version of the above routine.
         */
        public int session_add_request(Request req)
        {
            int rc = PlcTag.PLCTAG_STATUS_OK;

            //pdebug(DEBUG_INFO, "Starting. sess=%p, req=%p", sess, req);

            //critical_block(sess->mutex) {
                rc = session_add_request_unsafe(req);
            //}

//*HR*            //cond_signal(sess->wait_cond);

            //pdebug(DEBUG_INFO, "Done.");

            return rc;
        }

        /*
 * session_add_request_unsafe
 *
 * You must hold the mutex before calling this!
 */
        int session_add_request_unsafe(Request req)
        {
            int rc = PlcTag.PLCTAG_STATUS_OK;

            //pdebug(DEBUG_DETAIL, "Starting.");

            /*if (!session)
            {
                pdebug(DEBUG_WARN, "Session is null!");
                return PLCTAG_ERR_NULL_PTR;
            }*/

            //req = rc_inc(req);

            /*if (!req)
            {
                pdebug(DEBUG_WARN, "Request is either null or in the process of being deleted.");
                return PLCTAG_ERR_NULL_PTR;
            }*/

            /* make sure the request points to the session */

            /* insert into the requests vector */
            requests.Add(req);
            //vector_put(session->requests, vector_length(session->requests), req);

            //pdebug(DEBUG_DETAIL, "Total requests in the queue: %d", vector_length(session->requests));

            //pdebug(DEBUG_DETAIL, "Done.");

            return rc;
        }

        enum session_state_t {
            SESSION_OPEN_SOCKET_START, SESSION_OPEN_SOCKET_WAIT, SESSION_REGISTER,
            SESSION_SEND_FORWARD_OPEN, SESSION_RECEIVE_FORWARD_OPEN, SESSION_IDLE,
            SESSION_DISCONNECT, SESSION_UNREGISTER, SESSION_CLOSE_SOCKET,
            SESSION_START_RETRY, SESSION_WAIT_RETRY, SESSION_WAIT_RECONNECT
        }
        
        static void session_handler(Object arg)
        {
            //Thread.Sleep(1000);
            Session /*ab_session_p*/ session = (Session) arg;
            int rc = PlcTag.PLCTAG_STATUS_OK;
            session_state_t state = session_state_t.SESSION_OPEN_SOCKET_START;
            Int64 timeout_time = 0;
            Int64 wait_until_time = 0;
            Int64 auto_disconnect_time = DateTime.Now.Millisecond /*time_ms()*/ + SESSION_DISCONNECT_TIMEOUT;
            int auto_disconnect = 0;


            //pdebug(DEBUG_INFO, "Starting thread for session %p", session);

            while (session.terminating == 0)
            {
                /* how long should we wait if nothing wakes us? */
                wait_until_time = DateTime.Now.Millisecond /*time_ms()*/ + SESSION_IDLE_WAIT_TIME;

                /*
                 * Do this on every cycle.   This keeps the queue clean(ish).
                 *
                 * Make sure we get rid of all the aborted requests queued.
                 * This keeps the overall memory usage lower.
                 */

                //pdebug(DEBUG_SPEW, "Critical block.");
                session.mutex.mutex_lock() ;
                lock (/*critical_block(*/session.mutex) {
                    session.purge_aborted_requests_unsafe(/*session*/);
                }
                session.mutex.mutex_unlock();

                switch (state)
                {
                    case session_state_t.SESSION_OPEN_SOCKET_START:
                        //pdebug(DEBUG_DETAIL, "in SESSION_OPEN_SOCKET_START state.");

                        /* we must connect to the gateway*/
                        rc = session.session_open_socket(/*session*/);
                        if (rc != PlcTag.PLCTAG_STATUS_OK && rc != PlcTag.PLCTAG_STATUS_PENDING)
                        {
                            //pdebug(DEBUG_WARN, "session connect failed %s!", plc_tag_decode_error(rc));
                            state = session_state_t.SESSION_CLOSE_SOCKET;
                        }
                        else
                        {
                            if (rc == PlcTag.PLCTAG_STATUS_OK)
                            {
                                /* bump auto disconnect time into the future so that we do not accidentally disconnect immediately. */
                                auto_disconnect_time = DateTime.Now.Millisecond /*time_ms()*/ + SESSION_DISCONNECT_TIMEOUT;

                                //pdebug(DEBUG_DETAIL, "Connect complete immediately, going to state SESSION_REGISTER.");

                                state = session_state_t.SESSION_REGISTER;
                            }
                            else
                            {
                                //pdebug(DEBUG_DETAIL, "Connect started, going to state SESSION_OPEN_SOCKET_WAIT.");

                                state = session_state_t.SESSION_OPEN_SOCKET_WAIT;
                            }
                        }

                        /* in all cases, don't wait. */
                        //cond_signal(session->wait_cond);
                        //session.wait_cond.cond_signal();

                        break;

                    case session_state_t.SESSION_OPEN_SOCKET_WAIT:
                        //pdebug(DEBUG_DETAIL, "in SESSION_OPEN_SOCKET_WAIT state.");

                        /* we must connect to the gateway */
                        rc = session.socket_connect_tcp_check(/*session.sock,*/ 20); /* MAGIC */
                        if (rc == PlcTag.PLCTAG_STATUS_OK)
                        {
                            /* connected! */
                            //pdebug(DEBUG_INFO, "Socket connection succeeded.");

                            /* calculate the disconnect time. */
                            auto_disconnect_time = DateTime.Now.Millisecond /*time_ms()*/ + SESSION_DISCONNECT_TIMEOUT;

                            state = session_state_t.SESSION_REGISTER;
                        }
                        else if (rc == PlcTag.PLCTAG_ERR_TIMEOUT)
                        {
                            //pdebug(DEBUG_DETAIL, "Still waiting for connection to succeed.");

                            /* don't wait more.  The TCP connect check will wait in select(). */
                        }
                        else
                        {
                            //pdebug(DEBUG_WARN, "Session connect failed %s!", plc_tag_decode_error(rc));
                            state = session_state_t.SESSION_CLOSE_SOCKET;
                        }
 
                        /* in all cases, don't wait. */
                        //cond_signal(session->wait_cond);
                        session.wait_cond.cond_signal();

                        break;

                    case session_state_t.SESSION_REGISTER:
                        //pdebug(DEBUG_DETAIL, "in SESSION_REGISTER state.");

                        if ((rc = session.session_register(/*session*/)) != PlcTag.PLCTAG_STATUS_OK)
                        {
                            //pdebug(DEBUG_WARN, "session registration failed %s!", plc_tag_decode_error(rc));
                            state = session_state_t.SESSION_CLOSE_SOCKET;
                        }
                        else
                        {
                            if (session.use_connected_msg)
                            {
                                state = session_state_t.SESSION_SEND_FORWARD_OPEN;
                            }
                            else
                            {
                                state = session_state_t.SESSION_IDLE;
                            }
                        }
                        //cond_signal(session->wait_cond);
                           session.wait_cond.cond_signal(); 
                        break;

                    case session_state_t.SESSION_SEND_FORWARD_OPEN:
                        //pdebug(DEBUG_DETAIL, "in SESSION_SEND_FORWARD_OPEN state.");

                        if ((rc = session.send_forward_open_request(/*session*/)) != PlcTag.PLCTAG_STATUS_OK)
                        {
                            //pdebug(DEBUG_WARN, "Send Forward Open failed %s!", plc_tag_decode_error(rc));
                            state = session_state_t.SESSION_UNREGISTER;
                        }
                        else
                        {
                            //pdebug(DEBUG_DETAIL, "Send Forward Open succeeded, going to SESSION_RECEIVE_FORWARD_OPEN state.");
                            state = session_state_t.SESSION_RECEIVE_FORWARD_OPEN;
                        }
                        //cond_signal(session->wait_cond);
 //*HR*                       session.wait_cond.cond_signal();
                        break;

                    case session_state_t.SESSION_RECEIVE_FORWARD_OPEN:
                        //pdebug(DEBUG_DETAIL, "in SESSION_RECEIVE_FORWARD_OPEN state.");

                        if ((rc = session.receive_forward_open_response(/*session*/)) != PlcTag.PLCTAG_STATUS_OK)
                        {
                             if (rc == PlcTag.PLCTAG_ERR_DUPLICATE)
                            {
                                //pdebug(DEBUG_DETAIL, "Duplicate connection error received, trying again with different connection ID.");
                                state = session_state_t.SESSION_SEND_FORWARD_OPEN;
                            }
                            else if (rc == PlcTag.PLCTAG_ERR_TOO_LARGE)
                            {
                                //pdebug(DEBUG_DETAIL, "Requested packet size too large, retrying with smaller size.");
                                state = session_state_t.SESSION_SEND_FORWARD_OPEN;
                            }
                            else if (rc == PlcTag.PLCTAG_ERR_UNSUPPORTED && !session.only_use_old_forward_open)
                            {
                                /* if we got an unsupported error and we are trying with ForwardOpenEx, then try the old command. */
                                //pdebug(DEBUG_DETAIL, "PLC does not support ForwardOpenEx, trying old ForwardOpen.");
                                session.only_use_old_forward_open = true;
                                state = session_state_t.SESSION_SEND_FORWARD_OPEN;
                            }
                            else
                            {
                                //pdebug(DEBUG_WARN, "Receive Forward Open failed %s!", plc_tag_decode_error(rc));
                                state = session_state_t.SESSION_UNREGISTER;
                            }
                        }
                        else
                        {
                            //pdebug(DEBUG_DETAIL, "Send Forward Open succeeded, going to SESSION_IDLE state.");
                            state = session_state_t.SESSION_IDLE;
                        }
                        //cond_signal(session->wait_cond);
                        session.wait_cond.cond_signal();
                        //session.tag_cond_wait.cond_signal();
                        break;

                    case session_state_t.SESSION_IDLE:
                        //pdebug(DEBUG_DETAIL, "in SESSION_IDLE state.");

                        /* if there is work to do, make sure we do not disconnect. */
                        lock /*critical_block*/(session.mutex) {
                            int num_reqs = session.requests.Count; // vector_length(session->requests);
                            if (num_reqs > 0)
                            {
                                //pdebug(DEBUG_DETAIL, "There are %d requests pending before cleanup and sending.", num_reqs);
                                auto_disconnect_time = DateTime.Now.Millisecond /*time_ms()*/ + SESSION_DISCONNECT_TIMEOUT;
                            }
                        }

                        if ((rc = session.process_requests()) != PlcTag.PLCTAG_STATUS_OK)
                        {
                            //pdebug(DEBUG_WARN, "Error while processing requests %s!", plc_tag_decode_error(rc));
                            if (session.use_connected_msg)
                            {
                                state = session_state_t.SESSION_DISCONNECT;
                            }
                            else
                            {
                                state = session_state_t.SESSION_UNREGISTER;
                            }
                            //cond_signal(session->wait_cond);
                            session.wait_cond.cond_signal();
                        }
 
                        /* check if we should disconnect */
                        if (auto_disconnect_time < DateTime.Now.Millisecond /*time_ms()*/)
                        {
                            //pdebug(DEBUG_DETAIL, "Disconnecting due to inactivity.");

                            auto_disconnect = 1;

                            if (session.use_connected_msg)
                            {
                                state = session_state_t.SESSION_DISCONNECT;
                            }
                            else
                            {
                                state = session_state_t.SESSION_UNREGISTER;
                            }
                            //cond_signal(session->wait_cond);
                            session.wait_cond.cond_signal();
                        }

                        /* if there is work to do, make sure we signal the condition var. */
                        lock /*critical_block*/(session.mutex) {
                            int num_reqs = session.requests.Count; // vector_length(session->requests);
                            if (num_reqs > 0)
                            {
                                //pdebug(DEBUG_DETAIL, "There are %d requests still pending after abort purge and sending.", num_reqs);
                                //cond_signal(session->wait_cond);
                                session.wait_cond.cond_signal();
                            }
                        }

                        break;

                    case session_state_t.SESSION_DISCONNECT:
                        //pdebug(DEBUG_DETAIL, "in SESSION_DISCONNECT state.");

  /*HR*                      if ((rc = perform_forward_close(session)) != PlcTag.PLCTAG_STATUS_OK)
                        {
                            //pdebug(DEBUG_WARN, "Forward close failed %s!", plc_tag_decode_error(rc));
                        }
  */
                        state = session_state_t.SESSION_UNREGISTER;
                        //cond_signal(session->wait_cond);
                        session.wait_cond.cond_signal();
                        break;

                    case session_state_t.SESSION_UNREGISTER:
                        //pdebug(DEBUG_DETAIL, "in SESSION_UNREGISTER state.");

 /*HR*                       if ((rc = session_unregister(session)) != PlcTag.PLCTAG_STATUS_OK)
                        {
                            //pdebug(DEBUG_WARN, "Unregistering session failed %s!", plc_tag_decode_error(rc));
                        }
 */
                        state = session_state_t.SESSION_CLOSE_SOCKET;
                        //cond_signal(session->wait_cond);
                        session.wait_cond.cond_signal();
                        break;

                    case session_state_t.SESSION_CLOSE_SOCKET:
                        //pdebug(DEBUG_DETAIL, "in SESSION_CLOSE_SOCKET state.");

 /*HR                       if ((rc = session_close_socket(session)) != PlcTag.PLCTAG_STATUS_OK)
                        {
                            //pdebug(DEBUG_WARN, "Closing session socket failed %s!", plc_tag_decode_error(rc));
                        }
¨*/
                        if (auto_disconnect != 0 )
                        {
                            state = session_state_t.SESSION_WAIT_RECONNECT;
                        }
                        else
                        {
                            state = session_state_t.SESSION_START_RETRY;
                        }
                        //cond_signal(session->wait_cond);
                        session.wait_cond.cond_signal();   
                        break;

                    case session_state_t.SESSION_START_RETRY:
                        //pdebug(DEBUG_DETAIL, "in SESSION_START_RETRY state.");

                        /* FIXME - make this a tag attribute. */
                        timeout_time = DateTime.Now.Millisecond /*time_ms()*/ + RETRY_WAIT_MS;

                        /* start waiting. */
                        state = session_state_t.SESSION_WAIT_RETRY;

                        //cond_signal(session->wait_cond);
                        session.wait_cond.cond_signal();
                        break;

                    case session_state_t.SESSION_WAIT_RETRY:
                        //pdebug(DEBUG_DETAIL, "in SESSION_WAIT_RETRY state.");

                        if (timeout_time < DateTime.Now.Millisecond /*time_ms()*/)
                        {
                            //pdebug(DEBUG_DETAIL, "Transitioning to SESSION_OPEN_SOCKET_START.");
                            state = session_state_t.SESSION_OPEN_SOCKET_START;
                            //cond_signal(session->wait_cond);
                            session.wait_cond.cond_signal();
                        }

                        break;

                    case session_state_t.SESSION_WAIT_RECONNECT:
                        /* wait for at least one request to queue before reconnecting. */
                        //pdebug(DEBUG_DETAIL, "in SESSION_WAIT_RECONNECT state.");

                        auto_disconnect = 0;

                        /* if there is work to do, reconnect.. */
                        //pdebug(DEBUG_SPEW, "Critical block.");
                        lock /*critical_block*/(session.mutex) {
                            if (/*vector_length(session->requests*/ session.requests.Count > 0)
                            {
                                //pdebug(DEBUG_DETAIL, "There are requests waiting, reopening connection to PLC.");

                                state = session_state_t.SESSION_OPEN_SOCKET_START;
                                //cond_signal(session->wait_cond);
                                session.wait_cond.cond_signal( );
                            }
                        }

                        break;


                    default:
                        //pdebug(DEBUG_ERROR, "Unknown state %d!", state);

                        /* FIXME - this logic is not complete.  We might be here without
                         * a connected session or a registered session. */
                        if (session.use_connected_msg)
                        {
                            state = session_state_t.SESSION_DISCONNECT;
                        }
                        else
                        {
                            state = session_state_t.SESSION_UNREGISTER;
                        }

                        //cond_signal(session->wait_cond);
                        session.wait_cond.cond_signal();
                        break;
                }

                /*
                 * give up the CPU a bit, but only if we are not
                 * doing some linked states.
                 */
                if (wait_until_time > 0)
                {
                    Int64 time_left = wait_until_time - DateTime.Now.Millisecond /*time_ms()*/;

                    if (time_left > 0)
                    {
                        //cond_wait(session->wait_cond, (int)time_left);
                        session.wait_cond.cond_wait((int) time_left );
                    }
                }
            }

            /*
             * One last time before we exit.
             */
            //pdebug(DEBUG_DETAIL, "Critical block.");
            lock /*critical_block*/(session.mutex) {
                session.purge_aborted_requests_unsafe(/*session*/);
            }

            //THREAD_RETURN(0);
        }

        /*
 * This must be called with the session mutex held!
 */
        int purge_aborted_requests_unsafe(/*ab_session_p session*/)
        {
            int purge_count = 0;
            Request /*ab_request_p*/ request = null;

            //pdebug(DEBUG_SPEW, "Starting.");

            /* remove the aborted requests. */
            for (int i = 0; i < requests.Count/*vector_length(session->requests)*/; i++)
            {
                request = requests.ElementAt(i); // vector_get(session->requests, i);

                /* filter out the aborts. */
                if (request != null && (request.abort_request != 0)) //->abort_request)
                {
                    purge_count++;

                    /* remove it from the queue. */
                    //vector_remove(session->requests, i);
                    requests.Remove(request);

                    /* set the debug tag to the owning tag. */
                    //debug_set_tag_id(request->tag_id);

                    //pdebug(DEBUG_DETAIL, "Session thread releasing aborted request %p.", request);

                    //request->status = PLCTAG_ERR_ABORT;
                    //request->request_size = 0;
                    //request->resp_received = 1;

                    /* release our hold on it. */
                    //request = rc_dec(request);

                    /* vector size has changed, back up one. */
                    i--;
                }
            }

            if (purge_count > 0)
            {
                //pdebug(DEBUG_DETAIL, "Removed %d aborted requests.", purge_count);
            }

            //pdebug(DEBUG_SPEW, "Done.");

            return purge_count;
        
        }

        /*
         * session_open_socket()
         *
         * Connect to the host/port passed via TCP.
         */

        int session_open_socket(/*ab_session_p Session session*/)
        {
            int rc = PlcTag.PLCTAG_STATUS_OK;
            String[] /*char***/ server_port = null;
            int port = 0;

            //pdebug(DEBUG_INFO, "Starting.");

            /* Open a socket for communication with the gateway. */
            sock = new Socket(SocketType.Stream,
                ProtocolType.Tcp);
            /*rc = socket_create(&(session->sock));

            if (rc != PLCTAG_STATUS_OK)
            {
                pdebug(DEBUG_WARN, "Unable to create socket for session!");
                return rc;
            }*/

            server_port = host.Split(':'); // str_split(session.host, ":");
            if (server_port == null || server_port.Length ==0)
            {
                //pdebug(DEBUG_WARN, "Unable to split server and port string!");
                return PlcTag.PLCTAG_ERR_BAD_CONFIG;
            }

            if (server_port[0] == null)
            {
                //pdebug(DEBUG_WARN, "Server string is malformed or empty!");
                //mem_free(server_port);
                return PlcTag.PLCTAG_ERR_BAD_CONFIG;
            }

            if (server_port.Length >1 && server_port[1] != null)
            {
                rc = Str.str_to_int(server_port[0],ref port);
                if (rc != PlcTag.PLCTAG_STATUS_OK)
                {
                    //pdebug(DEBUG_WARN, "Unable to extract port number from server string \"%s\"!", session->host);
                    //mem_free(server_port);
                    return PlcTag.PLCTAG_ERR_BAD_CONFIG;
                }

                //pdebug(DEBUG_DETAIL, "Using special port %d.", port);
            }
            else
            {
                port = Defs.AB_EIP_DEFAULT_PORT;

                //pdebug(DEBUG_DETAIL, "Using default port %d.", port);
            }

            rc = Utils.Sockets.socket_connect_tcp_start(ref /*session.*/sock, server_port[0], port);

            if (rc != PlcTag.PLCTAG_STATUS_OK && rc != PlcTag.PLCTAG_STATUS_PENDING)
            {
                //pdebug(DEBUG_WARN, "Unable to connect socket for session!");
                //mem_free(server_port);
                return rc;
            }

            if (server_port != null)
            {
                //mem_free(server_port);
            }

            //pdebug(DEBUG_INFO, "Done.");

            return rc;
        }


        public int socket_connect_tcp_check(/*sock_p sock,*/ int timeout_ms)
        {
            int rc = PlcTag.PLCTAG_STATUS_OK;
            //fd_set write_set;
            //fd_set err_set;
            //struct timeval tv;
            TimeSpan tv; 
            int select_rc = 0;

            //pdebug(DEBUG_DETAIL, "Starting.");

            if (sock == null) {
                //pdebug(DEBUG_WARN, "Null socket pointer passed!");
                return PlcTag.PLCTAG_ERR_NULL_PTR;
            }

            /* wait for the socket to be ready. */
            //tv.tv_sec = (long) (timeout_ms / 1000);
            //tv.tv_usec = (long) (timeout_ms % 1000) * (long) (1000);
            tv = TimeSpan.FromMilliseconds(timeout_ms);

            /* Windows reports connection errors on the exception/error socket set. */

            //HashSet <Socket> write_set = new System.Collections.Generic.HashSet<Socket|>();
            //HashSet<Socket> err_set = new System.Collections.Generic.HashSet<Socket>();

            Socket[] write_set = new Socket[] { sock };
            Socket[] err_set = new Socket[] { sock };
            //write_set.Add(sock);
            //err_set.Add(sock);
            //FD_ZERO(&write_set);
            //FD_SET(sock->fd, &write_set);
            //FD_ZERO(&err_set);
            //FD_SET(sock->fd, &err_set);

            //select_rc = select((int)(sock->fd) + 1, NULL, &write_set, &err_set, &tv);
            Socket.Select(null, write_set,err_set, timeout_ms * 1000);

            foreach (Socket s in write_set)
            {
                if (s == sock)
                {
                    rc = PlcTag.PLCTAG_STATUS_OK;
                }
                else
                {
                    rc = PlcTag.PLCTAG_ERR_OPEN;
                }

            }

            /*if(select_rc == 1) {
                if(FD_ISSET(sock->fd, &write_set)) {
                    pdebug(DEBUG_DETAIL, "Socket is connected.");
                    rc = PLCTAG_STATUS_OK;
                } else if (FD_ISSET(sock->fd, &err_set))
                {
                    pdebug(DEBUG_WARN, "Error connecting!");
                    return PLCTAG_ERR_OPEN;
                }   
                else
                {
                    pdebug(DEBUG_WARN, "select() returned a 1, but no sockets are selected!");
                    return PLCTAG_ERR_OPEN;
                }
            } else if (select_rc == 0)
            {
                pdebug(DEBUG_DETAIL, "Socket connection not done yet.");
                rc = PLCTAG_ERR_TIMEOUT;
            }
            else
            {
                int err = WSAGetLastError();

                pdebug(DEBUG_WARN, "select() has error %d!", err);

                switch (err)
                {
                    case WSAENETDOWN: /* The network subsystem is down */
            /*            pdebug(DEBUG_WARN, "The network subsystem is down!");
                        return PLCTAG_ERR_OPEN;
                        break;

                    case WSANOTINITIALISED: /*Winsock was not initialized. */
            /*            pdebug(DEBUG_WARN, "WSAStartup() was not called to initialize the Winsock subsystem.!");
                        return PLCTAG_ERR_OPEN;
                        break;

                    case WSAEINVAL: /* The arguments to select() were bad. */
            /*            pdebug(DEBUG_WARN, "One or more of the arguments to select() were invalid!");
                        return PLCTAG_ERR_OPEN;
                        break;

                    case WSAEFAULT: /* No mem/resources for select. */
            /*            pdebug(DEBUG_WARN, "Insufficient memory or resources for select() to run!");
                        return PLCTAG_ERR_NO_MEM;
                        break;

                    case WSAEINTR: /* A blocking Windows Socket 1.1 call was canceled through WSACancelBlockingCall.  */
            /*            pdebug(DEBUG_WARN, "A blocking Winsock call was canceled!");
                        return PLCTAG_ERR_OPEN;
                        break;

                    case WSAEINPROGRESS: /* A blocking Windows Socket 1.1 call is in progress.  */
            /*            pdebug(DEBUG_WARN, "A blocking Winsock call is in progress!");
                        return PLCTAG_ERR_OPEN;
                        break;

                    case WSAENOTSOCK: /* One or more of the FDs in the set is not a socket. */
            /*            pdebug(DEBUG_WARN, "The fd in the FD set is not a socket!");
                        return PLCTAG_ERR_OPEN;
                        break;
            
                    default:
                        pdebug(DEBUG_WARN, "Unexpected err %d from select()!", err);
                        return PLCTAG_ERR_OPEN;
                        break;
                }
            }
            */
            //pdebug(DEBUG_DETAIL, "Done.");
        
            return rc;
        }

        int session_register(/*ab_session_p session*/)
        {
            eip_session_reg_req req;
            eip_encap resp;
            int rc = PlcTag.PLCTAG_STATUS_OK;

            //pdebug(DEBUG_INFO, "Starting.");

            /*
             * clear the session data.
             *
             * We use the receiving buffer because we do not have a request and nothing can
             * be coming in (we hope) on the socket yet.
             */
            req = new eip_session_reg_req();
            //mem_set(session->data, 0, sizeof(eip_session_reg_req));

            //req = (eip_session_reg_req*)(session->data);

            /* fill in the fields of the request */
            req.encap_command = /*h2le16(*/Defs.AB_EIP_REGISTER_SESSION/*)*/;
            req.encap_length = (ushort)(/*h2le16(*/eip_session_reg_req.size/*) */-  eip_encap.encap_size)/*)*/;
            req.encap_session_handle = /*h2le32(/*session->session_handle*/ 0/*)*/;
            req.encap_status = /*h2le32(*/0/*)*/;
            req.encap_sender_context = /*h2le64((uint64_t)*/0/*)*/;
            req.encap_options = /*h2le32(*/0/*)*/;

            req.eip_version = /*h2le16(*/Defs.AB_EIP_VERSION/*)*/;
            req.option_flags = /*h2le16(*/0/*)*/;

            /*
             * socket ops here are _ASYNCHRONOUS_!
             *
             * This is done this way because we do not have everything
             * set up for a request to be handled by the thread.  I think.
             */
            data = req.getData() ;
            /* send registration to the gateway */
            data_size = eip_session_reg_req.size;
            data_offset = 0;

            rc = send_eip_request(/*session,*/ SESSION_DEFAULT_TIMEOUT);
            if (rc != PlcTag.PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_WARN, "Error sending session registration request %s!", plc_tag_decode_error(rc));
                return rc;
            }

            /* get the response from the gateway */
            rc = recv_eip_response(/*session,*/ SESSION_DEFAULT_TIMEOUT);
            if (rc != PlcTag.PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_WARN, "Error receiving session registration response %s!", plc_tag_decode_error(rc));
                return rc;
            }

            /* encap header is at the start of the buffer */
            //resp = (eip_encap*)(session->data);

            resp = eip_encap.createFromData(data);

            /* check the response status */
            if (/*le2h16(*/resp.encap_command != Defs.AB_EIP_REGISTER_SESSION)
            {
                //pdebug(DEBUG_WARN, "EIP unexpected response packet type: %d!", resp->encap_command);
                return PlcTag.PLCTAG_ERR_BAD_DATA;
            }

            if (/*le2h32(*/resp.encap_status != Defs.AB_EIP_OK)
            {
                //pdebug(DEBUG_WARN, "EIP command failed, response code: %d", le2h32(resp->encap_status));
                return PlcTag.PLCTAG_ERR_REMOTE_ERR;
            }

            /*
             * after all that, save the session handle, we will
             * use it in future packets.
             */
            /*session->*/session_handle = /*le2h32(*/resp.encap_session_handle;

            //pdebug(DEBUG_INFO, "Done.");

            return PlcTag.PLCTAG_STATUS_OK;
        }


        int send_eip_request(/*ab_session_p session,*/ int timeout)
        {
            int rc = PlcTag.PLCTAG_STATUS_OK;
            Int64 timeout_time = 0;

            //pdebug(DEBUG_INFO, "Starting.");

            /*if (!session)
            {
                pdebug(DEBUG_WARN, "Session pointer is null.");
                return PLCTAG_ERR_NULL_PTR;
            }*/

            if (timeout > 0)
            {
                timeout_time = DateTime.Now.Millisecond /*time_ms()*/ + timeout;
            }
            else
            {
                timeout_time = /*INT64_MAX*/ Int64.MaxValue;
            }

            //pdebug(DEBUG_INFO, "Sending packet of size %d", session->data_size);
            //pdebug_dump_bytes(DEBUG_INFO, session->data, (int)(session->data_size));

            data_offset = 0;
            packet_count++;

            /* send the packet */
            do
            {
                byte[] data2 = new byte[data_size - data_offset];
                Array.Copy(data, data_offset, data2, 0,data_size - data_offset);
                rc = Sockets.socket_write(sock, ref data2,
                                  data2.Length,
                                  SOCKET_WAIT_TIMEOUT_MS);

                if (rc >= 0)
                {
                    data_offset += (uint) rc;
                }
                else
                {
                    if (rc == Lib.PlcTag.PLCTAG_ERR_TIMEOUT)
                    {
                        //pdebug(DEBUG_DETAIL, "Socket not yet ready to write.");
                        rc = 0;
                    }
                }

                /* give up the CPU if we still are looping */
                // if(!session->terminating && rc >= 0 && session->data_offset < session->data_size) {
                //     sleep_ms(1);
                // }
            } while (terminating == 0 && rc >= 0 && data_offset < data_size && timeout_time > /*time_ms()*/ DateTime.Now.Millisecond);

            if (terminating !=0)
            {
                //pdebug(DEBUG_WARN, "Session is terminating.");
                return PlcTag.PLCTAG_ERR_ABORT;
            }

            if (rc < 0)
            {
                //pdebug(DEBUG_WARN, "Error, %d, writing socket!", rc);
                return rc;
            }

            if (timeout_time <= DateTime.Now.Millisecond /*time_ms()*/)
            {
                //pdebug(DEBUG_WARN, "Timed out waiting to send data!");
                return PlcTag.PLCTAG_ERR_TIMEOUT;
            }

            //pdebug(DEBUG_INFO, "Done.");

            return PlcTag.PLCTAG_STATUS_OK;
        }


        /*
         * recv_eip_response
         *
         * Look at the passed session and read any data we can
         * to fill in a packet.If we already have a full packet,
         * punt.
         */
        public int recv_eip_response(/*ab_session_p session,*/ int timeout)
        {
            UInt32 data_needed = 0;
            int rc = PlcTag.PLCTAG_STATUS_OK|PlcTag.PLCTAG_STATUS_OK;
            Int64 timeout_time = 0;

            //pdebug(DEBUG_INFO, "Starting.");

            /*if (!session)
            {
                pdebug(DEBUG_WARN, "Called with null session!");
                return PLCTAG_ERR_NULL_PTR;
            }*/


            if (timeout > 0)
            {
                timeout_time = DateTime.Now.Millisecond /*time_ms()*/ + timeout;
            }
            else
            {
                timeout_time = Int64.MaxValue; //  INT64_MAX;
            }

            data_offset = 0;
            data_size = 0;
            data_needed = eip_encap.encap_size;  //sizeof(eip_encap);
            data = new byte[data_capacity];
            do
            {
                /*rc = socket_read(session->sock,
                                 session->data + session->data_offset,
                                 (int)(data_needed - session->data_offset),
                                 SOCKET_WAIT_TIMEOUT_MS);*/
                byte[] data2 = new byte[data_needed-data_offset];
                sock.Blocking = true;
                try
                    {
                    rc = Sockets.socket_read(sock, data2, (int) (data_needed - data_offset), SOCKET_WAIT_TIMEOUT_MS);
                    //rc = sock.Receive(data2, /* (int)(data_needed - data_offset),*/ SocketFlags.None);
                } catch 
                 {
                    rc = PlcTag.PLCTAG_ERR_TIMEOUT;

                }
                sock.Blocking = false;
                if (rc >= 0)
                {
                    Array.Copy(data2, 0, data, data_offset, rc);
                    data_offset += (UInt32)rc;

                    /*pdebug_dump_bytes(session->debug, session->data, session->data_offset);*/

                    /* recalculate the amount of data needed if we have just completed the read of an encap header */
                    if (data_offset >= eip_encap.encap_size)
                    {
                        data_needed = (UInt32)(eip_encap.encap_size) + eip_encap.createFromData(data).encap_length; // le2h16(((eip_encap*)(session->data))->encap_length));

                        if (data_needed > data_capacity)
                        {
                            //pdebug(DEBUG_WARN, "Packet response (%d) is larger than possible buffer size (%d)!", data_needed, session->data_capacity);
                            return PlcTag.PLCTAG_ERR_TOO_LARGE;
                        }
                    }
                }
                else
                {
                    if (rc == PlcTag.PLCTAG_ERR_TIMEOUT)
                    {
                        //pdebug(DEBUG_DETAIL, "Socket not yet ready to read.");
                        rc = 0;
                    }
                    else
                    {
                        /* error! */
                        //pdebug(DEBUG_WARN, "Error reading socket! rc=%d", rc);
                        return rc;
                    }
                }

                // /* did we get all the data? */
                // if(!session->terminating && session->data_offset < data_needed) {
                //     /* do not hog the CPU */
                //     sleep_ms(1);
                // }
            } while (terminating ==0 && data_offset < data_needed && timeout_time > DateTime.Now.Millisecond /*time_ms()*/);

            if (terminating != 0)
            {
                //pdebug(DEBUG_INFO, "Session is terminating, returning...");
                return PlcTag.PLCTAG_ERR_ABORT;
            }

            if (timeout_time <= DateTime.Now.Millisecond /*time_ms()*/)
            {
                //pdebug(DEBUG_WARN, "Timed out waiting for data to read!");
                return PlcTag.PLCTAG_ERR_TIMEOUT;
            }

            resp_seq_id = eip_encap.createFromData(data).encap_sender_context;
            data_size = data_needed;

            rc = PlcTag.PLCTAG_STATUS_OK;

            //pdebug(DEBUG_INFO, "request received all needed data (%d bytes of %d).", session->data_offset, data_needed);

            //pdebug_dump_bytes(DEBUG_INFO, session->data, (int)(session->data_offset));

            /* check status. */
            if (eip_encap.createFromData(data).encap_status != Defs.AB_EIP_OK)
            {
                rc = PlcTag.PLCTAG_ERR_BAD_STATUS;
            }

            //pdebug(DEBUG_INFO, "Done.");

            return rc;
        }


        int process_requests(/*ab_session_p session*/)
        {
            int rc = PlcTag.PLCTAG_STATUS_OK;
            Request /*ab_request_p*/ request = null;
            Request[] /*ab_request_p*/ bundled_requests = new Request[MAX_REQUESTS]; // = { NULL };
            int num_bundled_requests = 0;
            int remaining_space = 0;

            //debug_set_tag_id(0);

            //pdebug(DEBUG_SPEW, "Starting.");

            /*if (!session)
            {
                pdebug(DEBUG_WARN, "Null session pointer!");
                return PLCTAG_ERR_NULL_PTR;
            }*/

            //pdebug(DEBUG_SPEW, "Checking for requests to process.");

            rc = PlcTag.PLCTAG_STATUS_OK;
            request = null;
            data_size = 0;
            data_offset = 0;

            /* grab a request off the front of the list. */
            lock (mutex)
            { //critical_block(session->mutex) {
                int max_payload_size = GET_MAX_PAYLOAD_SIZE(/*session*/);

                // FIXME - no logging in a mutex!
                //pdebug(DEBUG_DETAIL, "FIXME: max payload size %d", max_payload_size);

                /* is there anything to do? */
                if (/*vector_length(*/requests.Count >0/*)*/)
                {
                    /* get rid of all aborted requests. */
                    purge_aborted_requests_unsafe(/*session*/);

                    /* if there are still requests after purging all the aborted requests, process them. */

                    /* how much space do we have to work with. */
                    remaining_space = max_payload_size - (int)/*sizeof(*/cip_multi_req_header.size/*)*/;

                    if (/*vector_length(session->*/requests.Count != 0/*)*/)
                    {
                        do
                        {
                            request = requests.ElementAt(0); // vector_get(session->requests, 0);

                            remaining_space = remaining_space - get_payload_size(request);

                            /*
                             * If we have a non-packable request, only queue it if it is the first one.
                             * If the request is packable, keep queuing as long as there is space.
                             */

                            if (num_bundled_requests == 0 || (request.allow_packing != 0 && remaining_space > 0))
                            {
                                //pdebug(DEBUG_DETAIL, "packed %d requests with remaining space %d", num_bundled_requests+1, remaining_space);
                                bundled_requests[num_bundled_requests] = request;
                                num_bundled_requests++;

                                /* remove it from the queue. */
                                //vector_remove(session->requests, 0);
                                requests.Remove(request);
                            }
                        } while (/*vector_length(session*/requests.Count!=0 && remaining_space > 0 && num_bundled_requests < MAX_REQUESTS && (request.allow_packing !=0));
                    }
                    else
                    {
                        //pdebug(DEBUG_DETAIL, "All requests in queue were aborted, nothing to do.");
                    }
                }
            }

            /* output debug display as no particular tag. */
            //debug_set_tag_id(0);

            if (num_bundled_requests > 0)
            {

                //pdebug(DEBUG_INFO, "%d requests to process.", num_bundled_requests);

                do
                {
                    /* copy and pack the requests into the session buffer. */
                    rc = pack_requests(bundled_requests, num_bundled_requests);
                    if (rc != PlcTag.PLCTAG_STATUS_OK)
                    {
                        //pdebug(DEBUG_WARN, "Error while packing requests, %s!", plc_tag_decode_error(rc));
                        break;
                    }

                    /* fill in all the necessary parts to the request. */
                    if ((rc = prepare_request(/*session*/)) != PlcTag.PLCTAG_STATUS_OK)
                    {
                        //pdebug(DEBUG_WARN, "Unable to prepare request, %s!", plc_tag_decode_error(rc));
                        break;
                    }

                    /* send the request */
                    if ((rc = send_eip_request(/*session,*/ SESSION_DEFAULT_TIMEOUT)) != PlcTag.PLCTAG_STATUS_OK)
                    {
                        //pdebug(DEBUG_WARN, "Error sending packet %s!", plc_tag_decode_error(rc));
                        break;
                    }

                    /* wait for the response */
                    if ((rc = recv_eip_response(/*session,*/ SESSION_DEFAULT_TIMEOUT)) != PlcTag.PLCTAG_STATUS_OK)
                    {
                        //pdebug(DEBUG_WARN, "Error receiving packet response %s!", plc_tag_decode_error(rc));
                        break;
                    }

                    /*
                     * check the CIP status, but only if this is a bundled
                     * response.   If it is a singleton, then we pass the
                     * status back to the tag.
                     */
                    if (num_bundled_requests > 1)
                    {
                        //if (le2h16(((eip_encap*)(session->data))->encap_command) == AB_EIP_UNCONNECTED_SEND)
                        if (eip_encap.createFromData(data).encap_command == Defs.AB_EIP_UNCONNECTED_SEND)
                        {
/*HR*                            eip_cip_uc_resp* resp = (eip_cip_uc_resp*)(session->data);
                            pdebug(DEBUG_INFO, "Received unconnected packet with session sequence ID %llx", resp->encap_sender_context);

                            /* punt if we got an overall error or it is not a partial/bundled error. */
/*HR*                            if (resp->status != AB_EIP_OK && resp->status != AB_CIP_ERR_PARTIAL_ERROR)
                            {
                                rc = decode_cip_error_code(&(resp->status));
                                pdebug(DEBUG_WARN, "Command failed! (%d/%d) %s", resp->status, rc, plc_tag_decode_error(rc));
                                break;
                            }
*/                        }
                        //else if (le2h16(((eip_encap*)(session->data))->encap_command) == AB_EIP_CONNECTED_SEND)
                        else if (eip_encap.createFromData(data).encap_command == Defs.AB_EIP_CONNECTED_SEND)
                        {
/*HR*                            eip_cip_co_resp* resp = (eip_cip_co_resp*)(session->data);
                            pdebug(DEBUG_INFO, "Received connected packet with connection ID %x and sequence ID %u(%x)", le2h32(resp->cpf_orig_conn_id), le2h16(resp->cpf_conn_seq_num), le2h16(resp->cpf_conn_seq_num));

                            /* punt if we got an overall error or it is not a partial/bundled error. */
/*HR*                            if (resp->status != AB_EIP_OK && resp->status != AB_CIP_ERR_PARTIAL_ERROR)
                            {
                                rc = decode_cip_error_code(&(resp->status));
                                pdebug(DEBUG_WARN, "Command failed! (%d/%d) %s", resp->status, rc, plc_tag_decode_error(rc));
                                break;
                            }
*/                        }
                    }

                    /* copy the results back out. Every request gets a copy. */
                    for (int i = 0; i < num_bundled_requests; i++)
                    {
                        //debug_set_tag_id(bundled_requests[i]->tag_id);

                        rc = unpack_response(bundled_requests[i], i);
                        if (rc != PlcTag.PLCTAG_STATUS_OK)
                        {
                            //pdebug(DEBUG_WARN, "Unable to unpack response!");
                            break;
                        }

                        /* release our reference */
                        //bundled_requests[i] = rc_dec(bundled_requests[i]);
                    }
 
                    rc = PlcTag.PLCTAG_STATUS_OK;
                } while (false);

                /* problem? clean up the pending requests and dump everything. */
                if (rc != PlcTag.PLCTAG_STATUS_OK)
                {
                    for (int i = 0; i < num_bundled_requests; i++)
                    {
                        if (bundled_requests[i]!=null)
                        {
                            bundled_requests[i].status = rc;
                            bundled_requests[i].request_size = 0;
                            bundled_requests[i].resp_received = 1;

                            //bundled_requests[i] = rc_dec(bundled_requests[i]);
                        }
                    }
                }

                /* tickle the main tickler thread to note that we have responses. */
                //plc_tag_tickler_wake();
                tag_tickler_wait.cond_signal();
            }

            //debug_set_tag_id(0);

            //pdebug(DEBUG_SPEW, "Done.");

            return rc;
        }

        int get_payload_size(/*ab_request_p*/ Request request)
        {
            int request_data_size = 0;
            eip_encap header = eip_encap.createFromData /*(eip_encap*)*/(request.data);
            eip_cip_co_req co_req = null;

            //pdebug(DEBUG_DETAIL, "Starting.");

            if (/*le2h16(*/header.encap_command == Defs.AB_EIP_CONNECTED_SEND)
    {
                co_req = eip_cip_co_req.createFromReq /*(eip_cip_co_req*)*/ (request/*.data*/);
        /* get length of new request */
        request_data_size = /*le2h16(*/ co_req.cpf_cdi_item_length /*)*/
                            - 2  /* for connection sequence ID */
                            + 2  /* for multipacket offset */
                            ;
    }
    else
    {
        // pdebug(DEBUG_DETAIL, "Not a supported type EIP packet type %d to get the payload size.", le2h16(header->encap_command));
        request_data_size = int.MaxValue;//INT_MAX;
    }

    //pdebug(DEBUG_DETAIL, "Done.");

    return request_data_size;
}



        int pack_requests(/*ab_session_p session,*/ Request[] requests, int num_requests)
        {
            eip_cip_co_req new_req = null;
            eip_cip_co_req packed_req = null;
            /* FIXME - is this the right way to check? */
            int header_size = 0;
            cip_multi_req_header multi_header = null;
            int current_offset = 0;
            byte[] pkt_start = null;
            int pkt_len = 0;
            int first_pkt_data = 0; // null;
            byte[] next_pkt_data = null;

            //pdebug(DEBUG_INFO, "Starting.");

            //debug_set_tag_id(requests[0]->tag_id);

            /* get the header info from the first request. Just copy the whole thing. */
            //mem_copy(session->data, requests[0]->data, requests[0]->request_size);
            Array.Copy(requests[0].data, data, requests[0].request_size);

            /*session->*/data_size = /*(uint32_t)*/ (UInt32) requests[0].request_size;

            /* special case the case where there is just one request. */
            if (num_requests == 1)
            {
                //pdebug(DEBUG_INFO, "Only one request, so done.");

                //debug_set_tag_id(0);

                return PlcTag.PLCTAG_STATUS_OK;
            }

            /* set up multi-packet header. */

            header_size = (int)(/*sizeof(*/cip_multi_req_header.size)
                                + (/*sizeof(uint16_le)*/ 2 * /*(size_t)*/num_requests/*)*/); /* offsets for each request. */

            //pdebug(DEBUG_INFO, "header size %d", header_size);

            //packed_req = (eip_cip_co_req*)(session->data);
            packed_req = eip_cip_co_req.createFromData(data);


            /* make room in the request packet in the session for the header. */
            //pkt_start = (uint8_t*)(&packed_req->cpf_conn_seq_num) + sizeof(packed_req->cpf_conn_seq_num);
            //pkt_len = (int)le2h16(packed_req->cpf_cdi_item_length) - (int)sizeof(packed_req->cpf_conn_seq_num);

            
            pkt_len = packed_req.cpf_cdi_item_length -2; // (int)sizeof(packed_req->cpf_conn_seq_num);
            pkt_start = new byte[pkt_len];


            //pdebug(DEBUG_INFO, "packet 0 is of length %d.", pkt_len);

            /* point to where we want the current packet to start. */
            first_pkt_data = /* pkt_start +*/ header_size;

            /* move the data over to make room */
            //mem_move(first_pkt_data, pkt_start, pkt_len);
            //Array.Copy(data, pkt_start, pkt_len );

            /* now fill in the header. Use pkt_start as it is pointing to the right location. */
            //multi_header = (cip_multi_req_header*)pkt_start;
            multi_header = new cip_multi_req_header();
            multi_header.service_code = Defs.AB_EIP_CMD_CIP_MULTI;
            multi_header.req_path_size = 0x02; /* length of path in words */
            multi_header.req_path[0] = 0x20; /* Class */
            multi_header.req_path[1] = 0x02; /* CM */
            multi_header.req_path[2] = 0x24; /* Instance */
            multi_header.req_path[3] = 0x01; /* #1 */
            multi_header.request_count = (UInt16)/*h2le16((uint16_t)*/num_requests;

            

            /* set up the offset for the first request. */
            current_offset = (int) 2/*(sizeof(uint16_le)*/ + /*(sizeof(uint16_le)*/ 2 * /*(size_t)*/num_requests;
            multi_header.request_offsets[0] = (UInt16) /*h2le16((uint16_t)*/current_offset;
            
            Array.Copy(multi_header.encodedData(), 0, data, first_pkt_data, pkt_len);

 //*HR*           //next_pkt_data = first_pkt_data + pkt_len;
            current_offset = current_offset + pkt_len;

            /* now process the rest of the requests. */
            for (int i = 1; i < num_requests; i++)
            {
                //debug_set_tag_id(requests[i]->tag_id);

                /* set up the offset */
                multi_header.request_offsets[i] = (UInt16) /*h2le16((uint16_t)*/current_offset;

                /* get a pointer to the request. */
                new_req = new eip_cip_co_req(); // (eip_cip_co_req*)(requests[i]->data);

                /* calculate the request start and length */
                //pkt_start = (uint8_t*)(&new_req->cpf_conn_seq_num) + sizeof(new_req->cpf_conn_seq_num);
                pkt_len = (int)/*le2h16(*/new_req.cpf_cdi_item_length - 2 /*(int)sizeof(new_req->cpf_conn_seq_num)*/;

                //pdebug(DEBUG_INFO, "packet %d is of length %d.", i, pkt_len);

                /* copy the request into the session buffer. */
                //mem_copy(next_pkt_data, pkt_start, pkt_len);

                /* calculate the next packet info. */
//*HR*                next_pkt_data += pkt_len;
                current_offset += pkt_len;


            }

            /* stitch up the CPF packet length */
            /* ???? */
            packed_req.cpf_cdi_item_length = 0;  //*(UInt16) h2le16((uint16_t)(next_pkt_data*/ - 1 ;   //(uint8_t*)(&packed_req->cpf_conn_seq_num)));

            /* stick up the EIP packet length */
 //*HR*           packed_req.encap_length = h2le16((uint16_t)((size_t)(next_pkt_data - session->data) - sizeof(eip_encap)));

            /* set the total data size */
 //*HR*           session->data_size = (uint32_t) (next_pkt_data - session->data);

            //debug_set_tag_id(0);

            //pdebug(DEBUG_INFO, "Done.");

            return PlcTag.PLCTAG_STATUS_OK;
        }


        int prepare_request(/*ab_session_p session*/)
        {
            eip_encap encap = null; // NULL;
            int payload_size = 0;

            //pdebug(DEBUG_INFO, "Starting.");

            //encap = (eip_encap*)(session->data);
            encap = eip_encap.createFromData(data);
            payload_size = (int)data_size - (int)eip_encap.encap_size/*sizeof(eip_encap)*/;

            
            /*if (!session)
            {
                pdebug(DEBUG_WARN, "Called with null session!");
                return PLCTAG_ERR_NULL_PTR;
            }*/

            /* fill in the fields of the request. */

            encap.encap_length = /*h2le16((uint16_t)*/ (UInt16) payload_size;
            encap.encap_session_handle = /*h2le32(session->*/ session_handle;
            encap.encap_status = 0;  //h2le32(0);
            encap.encap_options = 0;   //h2le32(0);

            /* set up the session sequence ID for this transaction */
            if (/*le2h16(*/encap.encap_command == Defs.AB_EIP_UNCONNECTED_SEND)
            {
                /* get new ID */
                session_seq_id++;

                //request->session_seq_id = session->session_seq_id;
                encap.encap_sender_context = /*h2le64(session->*/session_seq_id; /* link up the request seq ID and the packet seq ID */

                //pdebug(DEBUG_INFO, "Preparing unconnected packet with session sequence ID %llx", session->session_seq_id);
            }
            else if (/*le2h16(*/encap.encap_command == Defs.AB_EIP_CONNECTED_SEND)
            {
                Array.Copy(encap.getData(), 0, data, 0, encap.data.Length);
                //eip_cip_co_req conn_req = (eip_cip_co_req*)(session->data);
                eip_cip_co_req conn_req = eip_cip_co_req.createFromData(data);

                //pdebug(DEBUG_DETAIL, "cpf_targ_conn_id=%x", session->targ_connection_id);

                /* set up the connection information */
                conn_req.cpf_targ_conn_id = /*h2le32(session->*/targ_connection_id;

                conn_seq_num++;
                conn_req.cpf_conn_seq_num = /*h2le16(session->*/conn_seq_num;
                byte[] data2 = conn_req.encodedData();
                Array.Copy(data2, 0, data, 0, data2.Length);

                //pdebug(DEBUG_INFO, "Preparing connected packet with connection ID %x and sequence ID %u(%x)", session->orig_connection_id, session->conn_seq_num, session->conn_seq_num);
            }
            else
            {
                //pdebug(DEBUG_WARN, "Unsupported packet type %x!", le2h16(encap->encap_command));
                return PlcTag.PLCTAG_ERR_UNSUPPORTED;
            }

            /* display the data */
            //pdebug(DEBUG_INFO, "Prepared packet of size %d", session->data_size);
            //pdebug_dump_bytes(DEBUG_INFO, session->data, (int)session->data_size);

            //pdebug(DEBUG_INFO, "Done.");

            return PlcTag.PLCTAG_STATUS_OK;
        }


        int send_forward_open_request(/*ab_session_p session*/)
        {
            int rc = PlcTag.PLCTAG_STATUS_OK;
            UInt16 max_payload;

            //pdebug(DEBUG_INFO, "Starting");

            //pdebug(DEBUG_DETAIL, "Flag prohibiting use of extended ForwardOpen is %d.", session->only_use_old_forward_open);

            max_payload = (UInt16) (/*session->*/only_use_old_forward_open ? /*session->*/fo_conn_size : /*session->*/fo_ex_conn_size);

            /* set the max payload guess if it is larger than the maximum possible or if it is zero. */
            /*session->*/max_payload_guess = ((/*session->*/max_payload_guess == 0) || (/*session->*/max_payload_guess > max_payload) ? max_payload : /*session->*/max_payload_guess);

            //pdebug(DEBUG_DETAIL, "Set Forward Open maximum payload size guess to %d bytes.", session->max_payload_guess);

            if (/*session->*/only_use_old_forward_open)
            {
                rc = send_old_forward_open_request(/*session*/);
            }
            else
            {
                rc = send_extended_forward_open_request(/*session*/);
            }

            //pdebug(DEBUG_INFO, "Done");

            return rc;
        }

        int send_old_forward_open_request(/*ab_session_p session*/)
        {
            eip_forward_open_request_t fo = null;
            int data;
            int rc = PlcTag.PLCTAG_STATUS_OK;

            //pdebug(DEBUG_INFO, "Starting");

            //mem_set(session->data, 0, (int)(sizeof(*fo) + session->conn_path_size));

            //fo = (eip_forward_open_request_t*)(session->data);
            fo = new eip_forward_open_request_t();

            /* point to the end of the struct */
            //data = (session->data) + sizeof(eip_forward_open_request_t);
            data = fo.size;

            /* set up the path information. */
            //mem_copy(data, session->conn_path, session->conn_path_size);
            if (conn_path_size > 0)
            {

                Array.Copy(conn_path, 0, this.data, fo.size, conn_path_size);
            }
            data += /*session->*/conn_path_size;


            /* fill in the static parts */

            /* encap header parts */
            fo.encap_command = /*h2le16(*/Defs.AB_EIP_UNCONNECTED_SEND; /* 0x006F EIP Send RR Data command */
            fo.encap_length = /*h2le16(*/(UInt16)(data - fo.interface_handle_offs); /*(uint8_t*)(&fo->interface_handle))); /* total length of packet except for encap header */
            fo.encap_session_handle = /*h2le32(session->*/session_handle/*)*/;
            fo.encap_sender_context = /*h2le64(++session->*/++session_seq_id/*)*/;
            fo.router_timeout = 1; // h2le16(1);                       /* one second is enough ? */

            /* CPF parts */
            fo.cpf_item_count = 2; // h2le16(2);                  /* ALWAYS 2 */
            fo.cpf_nai_item_type = /*h2le16(*/Defs.AB_EIP_ITEM_NAI; /* null address item type */
            fo.cpf_nai_item_length = 0; //h2le16(0);             /* no data, zero length */
            fo.cpf_udi_item_type = /*h2le16(*/Defs.AB_EIP_ITEM_UDI;/*); /* unconnected data item, 0x00B2 */
            fo.cpf_udi_item_length = /*h2le16(*/(UInt16)(data - fo.cm_service_code_offs); // (uint8_t*)(&fo->cm_service_code))); /* length of remaining data in UC data item */


            
            /* Connection Manager parts */
            fo.cm_service_code = Defs.AB_EIP_CMD_FORWARD_OPEN; /* 0x54 Forward Open Request or 0x5B for Forward Open Extended */
            fo.cm_req_path_size = 2;                      /* size of path in 16-bit words */
            fo.cm_req_path[0] = 0x20;                     /* class */
            fo.cm_req_path[1] = 0x06;                     /* CM class */
            fo.cm_req_path[2] = 0x24;                     /* instance */
            fo.cm_req_path[3] = 0x01;                     /* instance 1 */

            /* Forward Open Params */
            fo.secs_per_tick = Defs.AB_EIP_SECS_PER_TICK;         /* seconds per tick, no used? */
            fo.timeout_ticks = Defs.AB_EIP_TIMEOUT_TICKS;         /* timeout = srd_secs_per_tick * src_timeout_ticks, not used? */
            fo.orig_to_targ_conn_id = 0; // h2le32(0);             /* is this right?  Our connection id on the other machines? */
            fo.targ_to_orig_conn_id = /*h2le32(session->*/orig_connection_id;/*); /* Our connection id in the other direction. */
            /* this might need to be globally unique */
            fo.conn_serial_number = /*h2le16(++(session->*/++conn_serial_number; /* our connection SEQUENCE number. */
            fo.orig_vendor_id = /*h2le16(*/Defs.AB_EIP_VENDOR_ID;               /* our unique :-) vendor ID */
            fo.orig_serial_number = /*h2le32(*/Defs.AB_EIP_VENDOR_SN;           /* our serial number. */
            fo.conn_timeout_multiplier = Defs.AB_EIP_TIMEOUT_MULTIPLIER;     /* timeout = mult * RPI */

            fo.orig_to_targ_rpi = /*h2le32(*/Defs.AB_EIP_RPI; /* us to target RPI - Request Packet Interval in microseconds */
                       

            /* screwy logic if this is a DH+ route! */
            if ((/*session->*/plc_type == PlcType.AB_PLC_PLC5 || /*session->*/plc_type == PlcType.AB_PLC_SLC || 
                /*session->*/plc_type == PlcType.AB_PLC_MLGX) && is_dhp==1)
            {
                fo.orig_to_targ_conn_params = /*h2le16(*/Defs.AB_EIP_PLC5_PARAM;
            }
            else
            {
                fo.orig_to_targ_conn_params = (ushort) (/*h2le16(*/Defs.AB_EIP_CONN_PARAM | /*session->*/max_payload_guess); /* packet size and some other things, based on protocol/cpu type */
            }

            fo.targ_to_orig_rpi = /*h2le32(*/Defs.AB_EIP_RPI; //); /* target to us RPI - not really used for explicit messages? */

            /* screwy logic if this is a DH+ route! */
            if ((/*session->*/plc_type == PlcType.AB_PLC_PLC5 || /*session->*/plc_type == PlcType.AB_PLC_SLC ||
                /*session->*/plc_type == PlcType.AB_PLC_MLGX) && /*session->*/is_dhp == 1)
            {
                fo.targ_to_orig_conn_params = /*h2le16(*/Defs.AB_EIP_PLC5_PARAM;
            }
            else
            {
                fo.targ_to_orig_conn_params = (ushort)(/*h2le16(*/Defs.AB_EIP_CONN_PARAM | /*session->*/max_payload_guess); /* packet size and some other things, based on protocol/cpu type */
            }

            fo.transport_class = Defs.AB_EIP_TRANSPORT_CLASS_T3; /* 0xA3, server transport, class 3, application trigger */
            fo.path_size = (byte)(/*session->*/conn_path_size / 2); /* size in 16-bit words */

            
            /* set the size of the request */
            data_size = (UInt32)data; // - (session->data))
            Array.Copy(fo.getData(), 0, this.data, 0, fo.size);

            rc = send_eip_request(/*session,*/ 0);

            //pdebug(DEBUG_INFO, "Done");

            return rc;
        }


        /* new version of Forward Open */
        int send_extended_forward_open_request(/*ab_session_p session*/)
        {
            eip_forward_open_request_ex_t fo = new eip_forward_open_request_ex_t();
            int data;
            int rc = PlcTag.PLCTAG_STATUS_OK;

            //pdebug(DEBUG_INFO, "Starting");

            //mem_set(session->data, 0, (int)(sizeof(*fo) + session->conn_path_size));
            data = fo.size; // + conn_path_size;

            // fo = (eip_forward_open_request_ex_t*)(session->data);
/*HR*            fo.setData(this.data);

            /* point to the end of the struct */
            //data = (session->data) + sizeof(*fo);

            /* set up the path information. */
            //mem_copy(data, session->conn_path, session->conn_path_size);
            if (conn_path_size > 0)
            {

                Array.Copy(conn_path, 0, this.data, fo.size, conn_path_size);
            }
            data += /*session->*/conn_path_size;

            /* fill in the static parts */

            /* encap header parts */
            fo.encap_command = /*h2le16(*/Defs.AB_EIP_UNCONNECTED_SEND/*)*/; /* 0x006F EIP Send RR Data command */
            fo.encap_length = /*h2le16(*/(UInt16)(data - fo.interface_handle_offs); /*(UInt8_t*)(&*fo->interface_handle))); /* total length of packet except for encap header */
            fo.encap_session_handle = /*h2le32(session->*/session_handle/*)*/;
            fo.encap_sender_context = /*h2le64(++session->*/++session_seq_id/*)*/;
            fo.router_timeout = 1; // h2le16(1);                       /* one second is enough ? */

            /* CPF parts */
            fo.cpf_item_count = 2; // h2le16(2);                  /* ALWAYS 2 */
            fo.cpf_nai_item_type = /*h2le16(*/Defs.AB_EIP_ITEM_NAI/*)*/; /* null address item type */
            fo.cpf_nai_item_length = 0; // h2le16(0);             /* no data, zero length */
            fo.cpf_udi_item_type = /*h2le16(*/ Defs.AB_EIP_ITEM_UDI/*)*/; /* unconnected data item, 0x00B2 */
            fo.cpf_udi_item_length = /*h2le16(*/(UInt16)(data - /*(uint8_t*)(&*/fo.cm_service_code_offs); /* length of remaining data in UC data item */

            /* Connection Manager parts */
            fo.cm_service_code = Defs.AB_EIP_CMD_FORWARD_OPEN_EX; /* 0x54 Forward Open Request or 0x5B for Forward Open Extended */
            fo.cm_req_path_size = 2;                      /* size of path in 16-bit words */
            fo.cm_req_path[0] = 0x20;                     /* class */
            fo.cm_req_path[1] = 0x06;                     /* CM class */
            fo.cm_req_path[2] = 0x24;                     /* instance */
            fo.cm_req_path[3] = 0x01;                     /* instance 1 */

            /* Forward Open Params */
            fo.secs_per_tick = Defs.AB_EIP_SECS_PER_TICK;         /* seconds per tick, no used? */
            fo.timeout_ticks = Defs.AB_EIP_TIMEOUT_TICKS;         /* timeout = srd_secs_per_tick * src_timeout_ticks, not used? */
            fo.orig_to_targ_conn_id = 0; // h2le32(0);             /* is this right?  Our connection id on the other machines? */
            fo.targ_to_orig_conn_id = /*h2le32(session->*/orig_connection_id/*)*/; /* Our connection id in the other direction. */
            /* this might need to be globally unique */
            fo.conn_serial_number = /*h2le16(++(session->*/++conn_serial_number; /* our connection ID/serial number. */
            fo.orig_vendor_id = /*h2le16(*/Defs.AB_EIP_VENDOR_ID;               /* our unique :-) vendor ID */
            fo.orig_serial_number = /*h2le32(*/ Defs.AB_EIP_VENDOR_SN;           /* our serial number. */
            fo.conn_timeout_multiplier = Defs.AB_EIP_TIMEOUT_MULTIPLIER;     /* timeout = mult * RPI */
            fo.orig_to_targ_rpi = /*h2le32(*/Defs.AB_EIP_RPI; /* us to target RPI - Request Packet Interval in microseconds */
            fo.orig_to_targ_conn_params_ex = /*h2le32(*/Defs.AB_EIP_CONN_PARAM_EX | /*session->*/max_payload_guess; /* packet size and some other things, based on protocol/cpu type */
            fo.targ_to_orig_rpi = /*h2le32(*/Defs.AB_EIP_RPI; /* target to us RPI - not really used for explicit messages? */
            fo.targ_to_orig_conn_params_ex = /*h2le32(*/Defs.AB_EIP_CONN_PARAM_EX | /*session->*/max_payload_guess; /* packet size and some other things, based on protocol/cpu type */
            fo.transport_class = Defs.AB_EIP_TRANSPORT_CLASS_T3; /* 0xA3, server transport, class 3, application trigger */
            fo.path_size = (byte)/*session->*/(conn_path_size / 2); /* size in 16-bit words */

            /* set the size of the request */
            /*session->*/
            data_size = (UInt32)data; // - (session->data));

            Array.Copy(fo.getData(), 0, this.data, 0, fo.size);

            rc = send_eip_request(SESSION_DEFAULT_TIMEOUT);

            //pdebug(DEBUG_INFO, "Done");

            return rc;
        }



        int receive_forward_open_response(/*ab_session_p session*/)
        {
            
            eip_forward_open_response_t fo_resp = new eip_forward_open_response_t();
            int rc = PlcTag.PLCTAG_STATUS_OK;

            //pdebug(DEBUG_INFO, "Starting");

            rc = recv_eip_response(/*session,*/ 0);
            if (rc != PlcTag.PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_WARN, "Unable to receive Forward Open response.");
                return rc;
            }

            //fo_resp = (eip_forward_open_response_t*)(session->data);
            fo_resp.setData(this.data);

           do
            {
                if (fo_resp.encap_command != Defs.AB_EIP_UNCONNECTED_SEND)
                {
                    //pdebug(DEBUG_WARN, "Unexpected EIP packet type received: %d!", fo_resp->encap_command);
                    rc = PlcTag.PLCTAG_ERR_BAD_DATA;
                    break;
                }

                if (fo_resp.encap_status != Defs.AB_EIP_OK)
                {
                    //pdebug(DEBUG_WARN, "EIP command failed, response code: %d", fo_resp->encap_status);
                    rc = PlcTag.PLCTAG_ERR_REMOTE_ERR;
                    break;
                }

                if (fo_resp.general_status != Defs.AB_EIP_OK)
                {
                    //pdebug(DEBUG_WARN, "Forward Open command failed, response code: %s (%d)", decode_cip_error_short(&fo_resp->general_status), fo_resp->general_status);
                    if (fo_resp.general_status == Defs.AB_CIP_ERR_UNSUPPORTED_SERVICE)
                    {
                        /* this type of command is not supported! */
                        //pdebug(DEBUG_WARN, "Received CIP command unsupported error from the PLC!");
                        rc = PlcTag.PLCTAG_ERR_UNSUPPORTED;
                    }
                    else
                    {
                        rc = PlcTag.PLCTAG_ERR_REMOTE_ERR;

                        if (fo_resp.general_status == 0x01 && fo_resp.status_size >= 2)
                        {
                            /* we might have an error that tells us the actual size to use. */
                            //uint8_t* data = &fo_resp->status_size;
                            //int extended_status = data[1] | (data[2] << 8);
                            int extended_status = fo_resp.status[0] | fo_resp.status[1] << 8;
                            UInt16 supported_size = (UInt16)((UInt16)fo_resp.status[2] | (UInt16)fo_resp.data[4] << 8);

                            if (extended_status == 0x109)
                            { /* MAGIC */
                                //pdebug(DEBUG_WARN, "Error from forward open request, unsupported size, but size %d is supported.", supported_size);
                                max_payload_guess = supported_size;
                                rc = PlcTag.PLCTAG_ERR_TOO_LARGE;
                            }
                            else if (extended_status == 0x100)
                            { /* MAGIC */
                                //pdebug(DEBUG_WARN, "Error from forward open request, duplicate connection ID.  Need to try again.");
                                rc = PlcTag.PLCTAG_ERR_DUPLICATE;
                            }
                            else
                            {
                                //pdebug(DEBUG_WARN, "CIP extended error %s (%s)!", decode_cip_error_short(&fo_resp->general_status), decode_cip_error_long(&fo_resp->general_status));
                            }
                        }
                        else
                        {
                            //pdebug(DEBUG_WARN, "CIP error code %s (%s)!", decode_cip_error_short(&fo_resp->general_status), decode_cip_error_long(&fo_resp->general_status));
                        }
                    }

                    break;
                }

                /* success! */
                targ_connection_id = fo_resp.orig_to_targ_conn_id;
                orig_connection_id = fo_resp.targ_to_orig_conn_id;

                max_payload_size = max_payload_guess;

                //pdebug(DEBUG_INFO, "ForwardOpen succeeded with our connection ID %x and the PLC connection ID %x with packet size %u.", session->orig_connection_id, session->targ_connection_id, session->max_payload_size);

                rc = PlcTag.PLCTAG_STATUS_OK;
            } while (false);

            //pdebug(DEBUG_INFO, "Done.");

            return rc;
        }

        int unpack_response(/*ab_session_p session,*/ Request /* ab_request_p*/ request, int sub_packet)
        {
            int rc = PlcTag.PLCTAG_STATUS_OK;
            eip_cip_co_resp packed_resp = eip_cip_co_resp.createFromData(/*session->*/data);
            eip_cip_co_resp unpacked_resp = null;
            //uint8_t* pkt_start = NULL;
            byte pkt_start = 0; 
            //uint8_t* pkt_end = NULL;
            byte pkt_end = 0;
            int new_eip_len = 0;

            //pdebug(DEBUG_INFO, "Starting.");

            /* clear out the request data. */
            //mem_set(request->data, 0, request->request_capacity);

            /* change what we do depending on the type. */
            if (packed_resp.reply_service != (Defs.AB_EIP_CMD_CIP_MULTI | Defs.AB_EIP_CMD_CIP_OK))
            {
                /* copy the data back into the request buffer. */
                new_eip_len = (int)/*sessioN.*/data_size;
                //pdebug(DEBUG_INFO, "Got single response packet.  Copying %d bytes unchanged.", new_eip_len);

                if (new_eip_len > request.request_capacity)
                {
                    int request_capacity = 0;

                    //pdebug(DEBUG_INFO, "Request buffer too small, allocating larger buffer.");

                    //critical_block(session->mutex) {
                        int max_payload_size = GET_MAX_PAYLOAD_SIZE(/*session*/);

                        // FIXME - no logging in a mutex!
                        // pdebug(DEBUG_DETAIL, "FIXME: max payload size %d", max_payload_size);

                        request_capacity = (int)(max_payload_size + EIP_CIP_PREFIX_SIZE);
                    //}

                    /* make sure it will fit. */
                    if (new_eip_len > request_capacity)
                    {
                        //pdebug(DEBUG_WARN, "something is very wrong, packet length is %d but allowable capacity is %d!", new_eip_len, request_capacity);
                        return PlcTag.PLCTAG_ERR_TOO_LARGE;
                    }

                    rc = session_request_increase_buffer(request, request_capacity);
                    if (rc != PlcTag.PLCTAG_STATUS_OK)
                    {
                        //pdebug(DEBUG_WARN, "Unable to increase request buffer size to %d bytes!", request_capacity);
                        return rc;
                    }
                }

                //mem_copy(request->data, session->data, new_eip_len);
/*HR*/                Array.Copy(data, request.data, new_eip_len);
            }
            else
            {
 /*HR*               cip_multi_resp_header* multi = (cip_multi_resp_header*)(&packed_resp->reply_service);
                uint16_t total_responses = le2h16(multi->request_count);
                int pkt_len = 0;

                /* this is a packed response. */
                //pdebug(DEBUG_INFO, "Got multiple response packet, subpacket %d", sub_packet);

                //pdebug(DEBUG_INFO, "Our result offset is %d bytes.", (int)le2h16(multi->request_offsets[sub_packet]));

/*HR                pkt_start = ((uint8_t*)(&multi->request_count) + le2h16(multi->request_offsets[sub_packet]));

                /* calculate the end of the data. */
 /*HR*               if ((sub_packet + 1) < total_responses)
                {
                    /* not the last response */
 /*HR*                   pkt_end = (uint8_t*)(&multi->request_count) + le2h16(multi->request_offsets[sub_packet + 1]);
                }
                else
                {
                    pkt_end = (session->data + le2h16(packed_resp->encap_length) + sizeof(eip_encap));
                }

                pkt_len = (int)(pkt_end - pkt_start);

                /* replace the request buffer if it is not big enough. */
 /*HR*               new_eip_len = pkt_len + (int)sizeof(eip_cip_co_generic_response);
                if (new_eip_len > request->request_capacity)
                {
                    int request_capacity = 0;

                    pdebug(DEBUG_INFO, "Request buffer too small, allocating larger buffer.");

                    critical_block(session->mutex) {
                        int max_payload_size = GET_MAX_PAYLOAD_SIZE(session);

                        // FIXME: no logging in a mutex!
                        // pdebug(DEBUG_DETAIL, "max payload size %d", max_payload_size);

                        request_capacity = (int)(max_payload_size + EIP_CIP_PREFIX_SIZE);
                    }

                    /* make sure it will fit. */
 /*HR*                   if (new_eip_len > request_capacity)
                    {
                        pdebug(DEBUG_WARN, "something is very wrong, packet length is %d but allowable capacity is %d!", new_eip_len, request_capacity);
                        return PLCTAG_ERR_TOO_LARGE;
                    }

                    rc = session_request_increase_buffer(request, request_capacity);
                    if (rc != PLCTAG_STATUS_OK)
                    {
                        pdebug(DEBUG_WARN, "Unable to increase request buffer size to %d bytes!", request_capacity);
                        return rc;
                    }
                }

                /* point to the response buffer in a structured way. */
 /*HR*               unpacked_resp = (eip_cip_co_resp*)(request->data);

                /* copy the header down */
 /*HR*               mem_copy(request->data, session->data, (int)sizeof(eip_cip_co_resp));

                /* size of the new packet */
 /*HR*               new_eip_len = (uint16_t)(((uint8_t*)(&unpacked_resp->reply_service) + pkt_len) /* end of the packet */
 /*HR*                                        - (uint8_t*)(request->data));                         /* start of the packet */

                /* now copy the packet over that. */
 /*HR*               mem_copy(&unpacked_resp->reply_service, pkt_start, pkt_len);

                /* stitch up the packet sizes. */
/*HR*                unpacked_resp->cpf_cdi_item_length = h2le16((uint16_t)(pkt_len + (int)sizeof(uint16_le))); /* extra for the connection sequence */
 /*HR*               unpacked_resp->encap_length = h2le16((uint16_t)(new_eip_len - (uint16_t)sizeof(eip_encap)));
 */           }

            //pdebug(DEBUG_INFO, "Unpacked packet:");
            //pdebug_dump_bytes(DEBUG_INFO, request->data, new_eip_len);

            /* notify the reading thread that the request is ready */
            //spin_block(&request->lock)
            {
                request.status = PlcTag.PLCTAG_STATUS_OK;
                request.request_size = new_eip_len;
                request.resp_received = 1;
            }

            //pdebug(DEBUG_DETAIL, "Done.");

            return PlcTag.PLCTAG_STATUS_OK;
        }

        public int session_request_increase_buffer(Request /*ab_request_p*/ request, int new_capacity)
        {
            byte[] old_buffer = null; // NULL;
            //byte new_buffer = 0; // NULL;

            //pdebug(DEBUG_DETAIL, "Starting.");

            //new_buffer = (uint8_t*)mem_alloc(new_capacity);
            byte[] new_buffer = new byte[new_capacity];
            /*if (!new_buffer)
            {
                pdebug(DEBUG_WARN, "Unable to allocate larger request buffer!");
                return PLCTAG_ERR_NO_MEM;
            }
            */
            //spin_block(&request->lock)
            {
                old_buffer = request.data;
                request.request_capacity = new_capacity;
                request.data = new_buffer;
            }

            //mem_free(old_buffer);

            //pdebug(DEBUG_DETAIL, "Done.");

            return PlcTag.PLCTAG_STATUS_OK;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: eliminar el estado administrado (objetos administrados)
                }
                session_teardown();
                // TODO: liberar los recursos no administrados (objetos no administrados) y reemplazar el finalizador
                // TODO: establecer los campos grandes como NULL
                disposedValue = true;
            }
        }

        // // TODO: reemplazar el finalizador solo si "Dispose(bool disposing)" tiene código para liberar los recursos no administrados
        // ~Session()
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

        protected void session_teardown()
        {
            int remaining_sessions = 0;

            //pdebug(DEBUG_INFO, "Starting.");

            /* flag all open sessions for termination */
            //pdebug(DEBUG_INFO, "Marking all open sessions for termination.");

            session_mutex = null;
            terminating = 1;
            /*if (session_mutex)
            {
                critical_block(session_mutex) {
                    if (sessions == NULL)
                    {
                        pdebug(DEBUG_INFO, "Session list is already destroyed.");

                        break;
                    }

                    remaining_sessions = vector_length(sessions);

                    for (int sess_index = 0; sess_index < remaining_sessions; sess_index++)
                    {
                        ab_session_p session = vector_get(sessions, sess_index);

                        if (session)
                        {
                            session->terminating = 1;
                        }
                    }
                }
            }*/

            /* flag the whole library shutting down. */
            //pdebug(DEBUG_INFO, "Setting library shutdown flag.");

            //atomic_set(&library_shutting_down, 1);

            /*if (sessions && session_mutex)
            {
                pdebug(DEBUG_DETAIL, "Waiting for sessions to terminate.");

                while (1)
                {
                    critical_block(session_mutex) {
                        remaining_sessions = vector_length(sessions);
                    }

                    /* wait for things to terminate. */
            /*        if (remaining_sessions > 0)
                    {
                        sleep_ms(50); // MAGIC
                    }
                    else
                    {
                        break;
                    }
                }

                pdebug(DEBUG_DETAIL, "Sessions all terminated.");

                vector_destroy(sessions);

                sessions = NULL;
            }*/

            //pdebug(DEBUG_DETAIL, "Destroying session mutex.");

            session_mutex = null;

            /*if (session_mutex)
            {
                mutex_destroy((mutex_p*)&session_mutex);
                session_mutex = NULL;
            }*/

            //atomic_set(&library_shutting_down, 0);

            //pdebug(DEBUG_INFO, "Done.");

        }

        public int session_get_max_payload(/*ab_session_p session*/)
        {
            int result = 0;

            /*if (!session)
            {
                pdebug(DEBUG_WARN, "Called with null session pointer!");
                return 0;
            }*/

            //critical_block(session->mutex) {
            lock (session_mutex)
            {
                result = GET_MAX_PAYLOAD_SIZE(); // (session);
            }//}

            //pdebug(DEBUG_DETAIL, "max payload size is %d bytes.", result);

            return result;
        }


    }



}
