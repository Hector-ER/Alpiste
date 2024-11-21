using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alpiste.Utils
{
    internal class Sockets
    {
        public static int socket_connect_tcp_start(ref Socket /*sock_p*/ s, String /*const char**/ host, int port)
        {
            int rc = Lib.PlcTag.PLCTAG_STATUS_OK;
            //IN_ADDR ips[MAX_IPS];
            int num_ips = 0;
            ///*struct*/ sockaddr_in gw_addr;
            int sock_opt = 1;
            //u_long non_blocking = 1;
            int i = 0;
            int done = 0;
            Socket /*SOCKET*/ fd;
            
            ///*struct*/ timeval timeout; /* used for timing out connections etc. */
            ///*struct*/ linger so_linger;

            //pdebug(DEBUG_DETAIL, "Starting.");

             /* Open a socket for communication with the gateway. */
            //fd = socket(AF_INET, SOCK_STREAM, 0/*IPPROTO_TCP*/);
            fd = new Socket(AddressFamily.InterNetwork/*ipEP.AddressFamily*/, SocketType.Stream,ProtocolType.Tcp);

            /* check for errors */
            /*if (fd< 0) {
                /*pdebug("Socket creation failed, errno: %d",errno);*/
            /*    return PLCTAG_ERR_OPEN;
            }*/

            /* set up our socket to allow reuse if we crash suddenly. */
            sock_opt = 1;

            /*if(setsockopt(fd, SOL_SOCKET, SO_REUSEADDR, (char*)&sock_opt, (int)sizeof(sock_opt))) {
                closesocket(fd);
                pdebug(DEBUG_WARN,"Error setting socket reuse option, errno: %d", errno);
                return PLCTAG_ERR_OPEN;
            }*/
            fd.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            //timeout.tv_sec = 10;
            //timeout.tv_usec = 0;

            fd.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 10000);
            /*if (setsockopt(fd, SOL_SOCKET, SO_RCVTIMEO, (char*)&timeout, (int)sizeof(timeout)))
            {
                closesocket(fd);
                pdebug(DEBUG_WARN, "Error setting socket receive timeout option, errno: %d", errno);
                return PLCTAG_ERR_OPEN;
            }*/

            fd.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 10000);

            /*if (setsockopt(fd, SOL_SOCKET, SO_SNDTIMEO, (char*)&timeout, (int)sizeof(timeout)))
            {
                closesocket(fd);
                pdebug(DEBUG_WARN, "Error setting socket send timeout option, errno: %d", errno);
                return PLCTAG_ERR_OPEN;
            }*/

            /* abort the connection on close. */
            //so_linger.l_onoff = 1;
            //so_linger.l_linger = 0;
            LingerOption lo = new LingerOption(true, 0);
            fd.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, lo);

            /*if (setsockopt(fd, SOL_SOCKET, SO_LINGER, (char*)&so_linger, (int)sizeof(so_linger)))
            {
                closesocket(fd);
                pdebug(DEBUG_ERROR, "Error setting socket close linger option, errno: %d", errno);
                return PLCTAG_ERR_OPEN;
            }*/

            /* figure out what address we are connecting to. */

            /* try a numeric IP address conversion first. */

            IPAddress ipAdd = IPAddress.Parse(host);
            IPAddress[] ips = new IPAddress[1];
            ips[0] = ipAdd;
            //if (inet_pton(AF_INET, host, (struct in_addr *)ips) > 0) {
            if (ipAdd != null ) { 

                //pdebug(DEBUG_DETAIL, "Found numeric IP address: %s", host);
                num_ips = 1;
            } else
            {
                //*HR* Ver               /*struct*/ addrinfo hints;
                //*HR* Ver               /*struct*/ addrinfo*res_head = NULL;
                //*HR* Ver              /*struct*/
                //*HR* Ver                addrinfo* res = NULL;
                //*HR* Ver
                //*HR* Ver                //mem_set(&ips, 0, sizeof(ips));
                //*HR* Ver                //mem_set(&hints, 0, sizeof(hints));

                //*HR* Ver                hints.ai_socktype = SOCK_STREAM; /* TCP */
                //*HR* Ver                hints.ai_family = AF_INET; /* IP V4 only */

                //*HR* Ver                if ((rc = getaddrinfo(host, NULL, &hints, &res_head)) != 0)
                //*HR* Ver                {
                //*HR* Ver                      pdebug(DEBUG_WARN, "Error looking up PLC IP address %s, error = %d\n", host, rc);

                //*HR* Ver                     if (res_head)
                //*HR* Ver                      {
                //*HR* Ver                          freeaddrinfo(res_head);
                //*HR* Ver                       }

                //*HR* Ver                      fd.Close();  // closesocket(fd);
                //*HR* Ver                      return Lib.PlcTag.PLCTAG_ERR_BAD_GATEWAY;
                //*HR* Ver                 }

                //*HR* Ver                 res = res_head;
                //*HR* Ver                 for (num_ips = 0; res && num_ips < MAX_IPS; num_ips++)
                //*HR* Ver                 {
                //*HR* Ver                     ips[num_ips].s_addr = ((/*struct*/ sockaddr_in *)(res->ai_addr))->sin_addr.s_addr;
                //*HR* Ver                     res = res->ai_next;
                //*HR* Ver                 }

                //*HR* Ver                 freeaddrinfo(res_head);

            }

            fd.Blocking = false;
                /* set the socket to non-blocking. */
            /*if (ioctlsocket(fd, FIONBIO, &non_blocking))
            {
                /*pdebug("Error getting socket options, errno: %d", errno);*/
            /*    closesocket(fd);
                return PLCTAG_ERR_OPEN;
            }*/

            /*
             * now try to connect to the remote gateway.  We may need to
             * try several of the IPs we have.
             */

            i = 0;
            done = 0;

            //memset((void*)&gw_addr, 0, sizeof(gw_addr));
            //gw_addr.sin_family = AF_INET;
            //gw_addr.sin_port = htons(port);

            do
            {
                /* try each IP until we run out or get a connection. */
                //gw_addr.sin_addr.s_addr = ips[i].s_addr;

                /*pdebug(DEBUG_DETAIL,"Attempting to connect to %s",inet_ntoa(*((struct in_addr *)&ips[i])));*/
                fd.Blocking = true;
                fd.Connect(ips[i]/*.s_addr*/, port);
                fd.Blocking = false;
                //rc = connect(fd, (struct sockaddr *)&gw_addr,sizeof(gw_addr));

                /* connect returns SOCKET_ERROR and a code of WSAEWOULDBLOCK on non-blocking sockets. */
                //if (rc == SOCKET_ERROR)
                if (!fd.Connected)
                {

                    /*int sock_err = WSAGetLastError();
                    if (sock_err == WSAEWOULDBLOCK)
                    
                    {
                        pdebug(DEBUG_DETAIL, "Socket connection attempt %d started successfully.", i);
                        rc = PLCTAG_STATUS_PENDING;
                        done = 1;
                    }
                    else
                    {
                        pdebug(DEBUG_WARN, "Error %d trying to start connection attempt %d process!  Trying next IP address.", sock_err, i);
    */                    i++;
    /*                }*/
                    
                }
                else
                {
                    //pdebug(DEBUG_DETAIL, "Socket connection attempt %d succeeded immediately.", i);
                    rc = Lib.PlcTag.PLCTAG_STATUS_OK;
                    done = 1;
                }
            } while (done == 0 && i < num_ips) ;

            if (done ==0 )
            {
                fd.Close(); // closesocket(fd);
                //pdebug(DEBUG_WARN, "Unable to connect to any gateway host IP address!");
                return Lib.PlcTag.PLCTAG_ERR_OPEN;
            }

            /* save the values */
            /*s.fd = fd;
            s.port = port;

            s.is_open = 1;
            */
            s = fd;
            //pdebug(DEBUG_DETAIL, "Done.");

            return rc;
        }

        public static int socket_write(Socket /*sock_p*/ s,ref byte[] /*uint8_t**/ buf, int size, int timeout_ms)
        {
            int rc;

            //pdebug(DEBUG_DETAIL, "Starting.");

            if (s == null)
            {
                //pdebug(DEBUG_WARN, "Socket pointer is null!");
                return Lib.PlcTag.PLCTAG_ERR_NULL_PTR;
            }

            /*if (!buf)
            {
                pdebug(DEBUG_WARN, "Buffer pointer is null!");
                return PLCTAG_ERR_NULL_PTR;
            }*/

            if (!s.Connected /* .is_open*/)
            {
                //pdebug(DEBUG_WARN, "Socket is not open!");
                return Lib.PlcTag.PLCTAG_ERR_READ;
            }

            if (timeout_ms < 0)
            {
                //pdebug(DEBUG_WARN, "Timeout must be zero or positive!");
                return Lib.PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            //rc = send(s->fd, (const char*)buf, size, (int)MSG_NOSIGNAL);
            rc = s.Send(buf, size, SocketFlags.None);
            /*if (rc < 0)
            {
                int err = WSAGetLastError();

                if (err == WSAEWOULDBLOCK)
                {
                    if (timeout_ms > 0)
                    {
                        pdebug(DEBUG_DETAIL, "Immediate write attempt did not succeed, now wait for select().");
                    }
                    else
                    {
                        pdebug(DEBUG_DETAIL, "Write wrote no data.");
                    }

                    rc = 0;
                }
                else
                {
                    pdebug(DEBUG_WARN, "socket write error rc=%d, errno=%d", rc, err);
                    return PLCTAG_ERR_WRITE;
                }
            }*/

            /* only wait if we have a timeout and no data. */
            if (rc == 0 && timeout_ms > 0)
            {
                //fd_set write_set;
                //TIMEVAL tv;
                int select_rc = 0;

                //tv.tv_sec = (long)(timeout_ms / 1000);
                //tv.tv_usec = (long)(timeout_ms % 1000) * (long)(1000);

                //FD_ZERO(&write_set);

                //FD_SET(s->fd, &write_set);

                //select_rc = select(1, NULL, &write_set, NULL, &tv);
                Socket[] sockets = new Socket[] { s };  
                Socket.Select(sockets, null, null, timeout_ms*1000);

                if (sockets.Length == 0) {

                    return Lib.PlcTag.PLCTAG_ERR_TIMEOUT;
                }


                /*if (select_rc == 1)
                {
                    if (FD_ISSET(s->fd, &write_set))
                    {
                        pdebug(DEBUG_DETAIL, "Socket can write data.");
                    }
                    else
                    {
                        pdebug(DEBUG_WARN, "select() returned but socket is not ready to write data!");
                        return PLCTAG_ERR_BAD_REPLY;
                    }
                }
                else if (select_rc == 0)
                {
                    pdebug(DEBUG_DETAIL, "Socket write timed out.");
                    return PLCTAG_ERR_TIMEOUT;
                }
                else
                {
                    int err = WSAGetLastError();

                    pdebug(DEBUG_WARN, "select() returned status %d!", select_rc);

                    switch (err)
                    {
                        case WSANOTINITIALISED: /* WSAStartup() not called first. */
     /*                       pdebug(DEBUG_WARN, "WSAStartUp() not called before calling Winsock functions!");
                            return PLCTAG_ERR_BAD_CONFIG;
                            break;

                        case WSAEFAULT: /* No mem for internal tables. */
/*                            pdebug(DEBUG_WARN, "Insufficient resources for select() to run!");
                            return PLCTAG_ERR_NO_MEM;
                            break;

                        case WSAENETDOWN: /* network subsystem is down. */
/*                            pdebug(DEBUG_WARN, "The network subsystem is down!");
                            return PLCTAG_ERR_BAD_DEVICE;
                            break;

                        case WSAEINVAL: /* timeout is invalid. */
/*                            pdebug(DEBUG_WARN, "The timeout is invalid!");
                            return PLCTAG_ERR_BAD_PARAM;
                            break;

                        case WSAEINTR: /* A blocking call wss cancelled. */
/*                            pdebug(DEBUG_WARN, "A blocking call was cancelled!");
                            return PLCTAG_ERR_BAD_CONFIG;
                            break;

                        case WSAEINPROGRESS: /* A blocking call is already in progress. */
/*                            pdebug(DEBUG_WARN, "A blocking call is already in progress!");
                            return PLCTAG_ERR_BAD_CONFIG;
                            break;

                        case WSAENOTSOCK: /* The descriptor set contains something other than a socket. */
/*                            pdebug(DEBUG_WARN, "The fd set contains something other than a socket!");
                            return PLCTAG_ERR_BAD_DATA;
                            break;

                        default:
                            pdebug(DEBUG_WARN, "Unexpected socket err %d!", err);
                            return PLCTAG_ERR_BAD_STATUS;
                            break;
                    }
                }

                /* try to write since select() said we could. */
                //rc = send(s->fd, (const char*)buf, size, (int)MSG_NOSIGNAL);
                rc = s.Send(buf, size, SocketFlags.None);
                if (rc < 0)
                {
                    //int err = WSAGetLastError();

                    /*if (err == WSAEWOULDBLOCK)
                    {
                        pdebug(DEBUG_DETAIL, "No data written.");
                        rc = 0;
                    }
                    else
                    {
                        pdebug(DEBUG_WARN, "socket write error rc=%d, errno=%d", rc, err);*/
                        return Lib.PlcTag.PLCTAG_ERR_WRITE;
                    //}
                }
            }

            //pdebug(DEBUG_DETAIL, "Done: result = %d.", rc);

            return rc;
        }

        public static int socket_read(Socket/*sock_p*/ s, byte[] /*uint8_t**/ buf, int size, int timeout_ms)
        {
            int rc;

            //pdebug(DEBUG_DETAIL, "Starting.");

            if (s==null)
            {
                //pdebug(DEBUG_WARN, "Socket pointer is null!");
                return Lib.PlcTag.PLCTAG_ERR_NULL_PTR;
            }

            if (buf==null)
            {
                //pdebug(DEBUG_WARN, "Buffer pointer is null!");
                return Lib.PlcTag.PLCTAG_ERR_NULL_PTR;
            }

            /*if (!s.is_open)
            {
                pdebug(DEBUG_WARN, "Socket is not open!");
                return PLCTAG_ERR_READ;
            }*/

            if (timeout_ms < 0)
            {
                //pdebug(DEBUG_WARN, "Timeout must be zero or positive!");
                return Lib.PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            /* try to read without waiting.   Saves a system call if it works. */
            //rc = recv(s->fd, (char*)buf, size, 0);
            rc = s.Receive(buf, size, SocketFlags.None);
            if (rc < 0)
            {
                /*int err = WSAGetLastError();

                if (err == WSAEWOULDBLOCK)
                {
                    if (timeout_ms > 0)
                    {
                        pdebug(DEBUG_DETAIL, "Immediate read attempt did not succeed, now wait for select().");
                    }
                    else
                    {
                        pdebug(DEBUG_DETAIL, "Read resulted in no data.");
                    }

                    rc = 0;
                }
                else
                {
                    pdebug(DEBUG_WARN, "socket read error rc=%d, errno=%d", rc, err);
                    return PLCTAG_ERR_READ;
                }*/
                return Lib.PlcTag.PLCTAG_ERR_READ;
            }

            /* only wait if we have a timeout and no data and no error. */
            if (rc == 0 && timeout_ms > 0)
            {
  /*HR*              fd_set read_set;
                TIMEVAL tv;
                int select_rc = 0;

                tv.tv_sec = (long)(timeout_ms / 1000);
                tv.tv_usec = (long)(timeout_ms % 1000) * (long)(1000);

                FD_ZERO(&read_set);

                FD_SET(s->fd, &read_set);

                select_rc = select(1, &read_set, NULL, NULL, &tv);
                if (select_rc == 1)
                {
                    if (FD_ISSET(s->fd, &read_set))
                    {
                        pdebug(DEBUG_DETAIL, "Socket can read data.");
                    }
                    else
                    {
                        pdebug(DEBUG_WARN, "select() returned but socket is not ready to read data!");
                        return PLCTAG_ERR_BAD_REPLY;
                    }
                }
                else if (select_rc == 0)
                {
                    pdebug(DEBUG_DETAIL, "Socket read timed out.");
                    return PLCTAG_ERR_TIMEOUT;
                }
                else
                {
                    int err = WSAGetLastError();

                    pdebug(DEBUG_WARN, "select() returned status %d!", select_rc);

                    switch (err)
                    {
                        case WSANOTINITIALISED: /* WSAStartup() not called first. */
/*HR*                           pdebug(DEBUG_WARN, "WSAStartUp() not called before calling Winsock functions!");
                           return PLCTAG_ERR_BAD_CONFIG;
                           break;

                       case WSAEFAULT: /* No mem for internal tables. */
/*HR*                           pdebug(DEBUG_WARN, "Insufficient resources for select() to run!");
                           return PLCTAG_ERR_NO_MEM;
                           break;

                       case WSAENETDOWN: /* network subsystem is down. */
/*HR*                           pdebug(DEBUG_WARN, "The network subsystem is down!");
                           return PLCTAG_ERR_BAD_DEVICE;
                           break;

                       case WSAEINVAL: /* timeout is invalid. */
/*HR*                            pdebug(DEBUG_WARN, "The timeout is invalid!");
                            return PLCTAG_ERR_BAD_PARAM;
                            break;

                        case WSAEINTR: /* A blocking call wss cancelled. */
/*HR*                           pdebug(DEBUG_WARN, "A blocking call was cancelled!");
                           return PLCTAG_ERR_BAD_CONFIG;
                           break;

                       case WSAEINPROGRESS: /* A blocking call is already in progress. */
/*HR*                           pdebug(DEBUG_WARN, "A blocking call is already in progress!");
                           return PLCTAG_ERR_BAD_CONFIG;
                           break;

                       case WSAENOTSOCK: /* The descriptor set contains something other than a socket. */
/*HR*                           pdebug(DEBUG_WARN, "The fd set contains something other than a socket!");
                           return PLCTAG_ERR_BAD_DATA;
                           break;

                       default:
                           pdebug(DEBUG_WARN, "Unexpected socket err %d!", err);
                           return PLCTAG_ERR_BAD_STATUS;
                           break;
                   }
               }

               /* select() returned saying we can read, so read. */
/*HR*               rc = recv(s->fd, (char*)buf, size, 0);
               if (rc < 0)
               {
                   int err = WSAGetLastError();

                   if (err == WSAEWOULDBLOCK)
                   {
                       rc = 0;
                   }
                   else
                   {
                       pdebug(DEBUG_WARN, "socket read error rc=%d, errno=%d", rc, err);
                       return PLCTAG_ERR_READ;
                   }
               }
 */          }

           //pdebug(DEBUG_DETAIL, "Done: result = %d.", rc);

           return rc;
       }


   }
}
