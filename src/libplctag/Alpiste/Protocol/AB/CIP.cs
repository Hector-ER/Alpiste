using Alpiste.Protocol.AB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alpiste.Lib;
using System.Xml.Linq;

namespace Alpiste.Protocol.AB
{
    static public  class CIP
    {
        public const int MAX_TAG_NAME = 260;
        public const int MAX_TAG_TYPE_INFO =  64;
        public static int cip_encode_path(String path, ref int needs_connection, PlcType plc_type, ref byte[] tmp_conn_path,  ref int tmp_conn_path_size, ref int _is_dhp, ref UInt16 dhp_dest)
        {
            //byte[] tmp_conn_path = new byte[10];
            int /*size_t*/ path_len = 0;
            int /*size_t*/ conn_path_index = 0;
            int /*size_t*/ path_index = 0;
            byte dhp_port = 0;
            byte dhp_src_node = 0;
            byte dhp_dest_node = 0;
            // uint8_t tmp_conn_path[MAX_CONN_PATH + MAX_IP_ADDR_SEG_LEN];
            int /*size_t*/ max_conn_path_size = (tmp_conn_path_size) - Session.MAX_IP_ADDR_SEG_LEN;

            //pdebug(DEBUG_DETAIL, "Starting");

            _is_dhp = (int)0;
            String s = path;
            path_len = s.Length;

            while(path != null && path_index < path_len && path[path_index] != null  && conn_path_index<max_conn_path_size) {
                /* skip spaces before each segment */
                while(path[path_index] == ' ') {
                    path_index++;
                }
                char c = path[path_index];
                if(c == ',') {
                    /* skip separators. */
                    //pdebug(DEBUG_DETAIL, "Skipping separator character '%c'.", (char) path[path_index]);

                    path_index++;
                } else if (match_numeric_segment(path, ref path_index, ref tmp_conn_path, ref conn_path_index) == PlcTag.PLCTAG_STATUS_OK)
                {
                    int a = 0;
                    //pdebug(DEBUG_DETAIL, "Found numeric segment.");
                }
                else if (match_ip_addr_segment(path, ref path_index, ref tmp_conn_path, ref conn_path_index) == PlcTag.PLCTAG_STATUS_OK)
                {
                    //pdebug(DEBUG_DETAIL, "Found IP address segment.");
                }
                else if (match_dhp_addr_segment(path, ref path_index, ref dhp_port, ref dhp_src_node, ref dhp_dest_node) == PlcTag.PLCTAG_STATUS_OK)
                {
                    //pdebug(DEBUG_DETAIL, "Found DH+ address segment.");

                    /* check if it is last. */
                    if (path_index < path_len)
                    {
                        //pdebug(DEBUG_WARN, "DH+ address must be the last segment in a path! %d %d", (int)(ssize_t)path_index, (int)(ssize_t)path_len);
                        return PlcTag.PLCTAG_ERR_BAD_PARAM;
                    }

                     _is_dhp = 1;
                }
                else
                {
                    /* unknown, cannot parse this! */
                    //pdebug(DEBUG_WARN, "Unable to parse remaining path string from position %d, \"%s\".", (int)(ssize_t)path_index, (char*)&path[path_index]);
                    return PlcTag.PLCTAG_ERR_BAD_PARAM;
                }
             }

            if (conn_path_index >= max_conn_path_size)
            {
                //pdebug(DEBUG_WARN, "Encoded connection path is too long (%d >= %d).", (int)(ssize_t)conn_path_index, max_conn_path_size);
                return PlcTag.PLCTAG_ERR_TOO_LARGE;
            }

            if (_is_dhp !=0  && (plc_type == PlcType.AB_PLC_PLC5 || plc_type == PlcType.AB_PLC_SLC ||
                plc_type == PlcType.AB_PLC_MLGX))
                {
                    /* DH+ bridging always needs a connection. */
                    needs_connection = 1;

                    /* add the special PCCC/DH+ routing on the end. */
                    tmp_conn_path[conn_path_index + 0] = 0x20;
                    tmp_conn_path[conn_path_index + 1] = 0xA6;
                    tmp_conn_path[conn_path_index + 2] = 0x24;
                    tmp_conn_path[conn_path_index + 3] = dhp_port;
                    tmp_conn_path[conn_path_index + 4] = 0x2C;
                    tmp_conn_path[conn_path_index + 5] = 0x01;
                    conn_path_index += 6;

                    dhp_dest = (UInt16) dhp_dest_node;
                }
                else if (_is_dhp ==0)
                {
                    if (needs_connection !=0)
                    {
                        //pdebug(DEBUG_DETAIL, "PLC needs connection, adding path to the router object.");

                        /*
                         * we do a generic path to the router
                         * object in the PLC.  But only if the PLC is
                         * one that needs a connection.  For instance a
                         * Micro850 needs to work in connected mode.
                         */
                    
                        tmp_conn_path[conn_path_index + 0] = 0x20;
                        tmp_conn_path[conn_path_index + 1] = 0x02;
                        tmp_conn_path[conn_path_index + 2] = 0x24;
                        tmp_conn_path[conn_path_index + 3] = 0x01;
                        conn_path_index += 4;
                    }

                    dhp_dest = 0;
                } 
                else
                {
                    /*
                     *we had the special DH+ format and it was
                     * either not last or not a PLC5/SLC.  That
                     * is an error.
                     */

                    dhp_dest = 0;

                    return PlcTag.PLCTAG_ERR_BAD_PARAM;
                }

                /*
                 * zero pad the path to a multiple of 16-bit
                 * words.
                 */
                //pdebug(DEBUG_DETAIL, "IOI size before %d", conn_path_index);
                if ((conn_path_index & 0x01) !=0)
                {
                    tmp_conn_path[conn_path_index] = 0;
                    conn_path_index++;
                }

            tmp_conn_path_size = (byte)conn_path_index;

            //pdebug(DEBUG_DETAIL, "Done");

            return PlcTag.PLCTAG_STATUS_OK;
        }

        static bool isdigit(char c)
        {
            return (c >= '0' && c <= '9');
        }

        public static int match_numeric_segment(String path, ref int /*size_t*/ path_index, ref byte[] conn_path, ref int /*size_t*/ conn_path_index)
        {
            int val = 0;
            int /*size_t*/ p_index = path_index;
            int /*size_t*/ c_index = conn_path_index;

            //pdebug(DEBUG_DETAIL, "Starting at position %d in string %s.", (int)(ssize_t)* path_index, path);

            while(p_index < path.Length && isdigit(path[p_index])) {
                val = (val* 10) + (path[p_index] - '0');
                p_index++;
            }

            /* did we match anything? */
            if(p_index == path_index) {
                //pdebug(DEBUG_DETAIL,"Did not find numeric path segment at position %d.", (int)(ssize_t) p_index);
                return PlcTag.PLCTAG_ERR_NOT_FOUND;
             }

            /* was the numeric segment valid? */
            if (val < 0 || val > 0x0F)
            {
                //pdebug(DEBUG_WARN, "Numeric segment in path at position %d is out of bounds!", (int)(ssize_t)(*path_index));
                return PlcTag.PLCTAG_ERR_OUT_OF_BOUNDS;
            }

            /* store the encoded segment data. */
            conn_path[c_index] = (byte)(val);
            c_index++;
            conn_path_index = c_index;

            /* skip trailing spaces */
            while (p_index < path.Length && path[p_index] == ' ')
            {
                p_index++;
            }

            //pdebug(DEBUG_DETAIL, "Remaining path \"%s\".", &path[p_index]);

            /* bump past our last read character. */
            path_index = p_index;

            //pdebug(DEBUG_DETAIL, "Done. Found numeric segment %d.", val);

            return PlcTag.PLCTAG_STATUS_OK;
        }

        /*
         * match symbolic IP address segments.
         *  18,10.206.10.14 - port 2/A -> 10.206.10.14
         *  19,10.206.10.14 - port 3/B -> 10.206.10.14
         */

        static public int match_ip_addr_segment(String path, ref int /*size_t*/ path_index, ref byte[] conn_path, ref int /*size_t*/ conn_path_index)
        {
            //byte[] addr_seg_len = null;
            byte addr_seg_len;
            int addr_seg_len_pos;
            int val = 0;
            int /*size_t*/ p_index = path_index;
            int /*size_t*/ c_index = conn_path_index;

            //pdebug(DEBUG_DETAIL, "Starting at position %d in string %s.", (int)(ssize_t)* path_index, path);

            /* first part, the extended address marker*/
            val = 0;
            while (p_index < path.Length && isdigit(path[p_index]))
            {
                val = (val * 10) + (path[p_index] - '0');
                p_index++;
            }

            if (val != 18 && val != 19)
            {
                //pdebug(DEBUG_DETAIL, "Path segment at %d does not match IP address segment.", (int)(ssize_t)* path_index);
                return PlcTag.PLCTAG_ERR_NOT_FOUND;
            }

            if (val == 18)
            {
                //pdebug(DEBUG_DETAIL, "Extended address on port A.");
            }
            else
            {
                //pdebug(DEBUG_DETAIL, "Extended address on port B.");
            }

            /* skip spaces */
            while (p_index < path.Length && path[p_index] == ' ')
            {
                p_index++;
            }

            /* is the next character a comma? */
            if (!(p_index < path.Length) || path[p_index] != ',')
            {
                //pdebug(DEBUG_DETAIL, "Not an IP address segment starting at position %d of path.  Remaining: \"%s\".", (int)(ssize_t)p_index, &path[p_index]);
                return PlcTag.PLCTAG_ERR_NOT_FOUND;
            }

            p_index++;

            /* start building up the connection path. */
            conn_path[c_index] = (byte)val;
            c_index++;

            /* point into the encoded path for the symbolic segment length. */
            //addr_seg_len = &conn_path[c_index];
            //*addr_seg_len = 0;
            addr_seg_len_pos = c_index;
            addr_seg_len = 0;
            c_index++;

            /* skip spaces */
            while (p_index < path.Length && path[p_index] == ' ')
            {
                p_index++;
            }

            /* get the first IP address digit. */
            val = 0;
            while (p_index < path.Length && isdigit(path[p_index]) && (addr_seg_len < (Session.MAX_IP_ADDR_SEG_LEN - 1)))
            {
                val = (val * 10) + (path[p_index] - '0');
                conn_path[c_index] = (byte)path[p_index];
                c_index++;
                p_index++;
                addr_seg_len++;
            }

            if (val < 0 || val > 255)
            {
                //pdebug(DEBUG_WARN, "First IP address part is out of bounds (0 <= %d < 256) for an IPv4 octet.", val);
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            //pdebug(DEBUG_DETAIL, "First IP segment: %d.", val);

            /* skip spaces */
            while (p_index < path.Length && path[p_index] == ' ')
            {
                p_index++;
            }

            /* is the next character a dot? */
            if (!(p_index < path.Length) || path[p_index] != '.')
            {
                //pdebug(DEBUG_DETAIL, "Unexpected character '%c' found at position %d in first IP address part.", path[p_index], p_index);
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            /* copy the dot. */
            conn_path[c_index] = (byte)path[p_index];
            c_index++;
            p_index++;
            (addr_seg_len)++;

            /* skip spaces */
            while (p_index < path.Length && path[p_index] == ' ')
            {
                p_index++;
            }

            /* get the second part. */
            val = 0;
            while (p_index < path.Length && isdigit(path[p_index]) && (addr_seg_len < (Session.MAX_IP_ADDR_SEG_LEN - 1)))
            {
                val = (val * 10) + (path[p_index] - '0');
                conn_path[c_index] = (byte)path[p_index];
                c_index++;
                p_index++;
                (addr_seg_len)++;
            }

            if (val < 0 || val > 255)
            {
                //pdebug(DEBUG_WARN, "Second IP address part is out of bounds (0 <= %d < 256) for an IPv4 octet.", val);
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            //pdebug(DEBUG_DETAIL, "Second IP segment: %d.", val);

            /* skip spaces */
            while (p_index < path.Length && path[p_index] == ' ')
            {
                p_index++;
            }

            /* is the next character a dot? */
            if (!(p_index < path.Length) || path[p_index] != '.')
            {
                //pdebug(DEBUG_DETAIL, "Unexpected character '%c' found at position %d in second IP address part.", path[p_index], p_index);
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            /* copy the dot. */
            conn_path[c_index] = (byte)path[p_index];
            c_index++;
            p_index++;
            (addr_seg_len)++;

            /* skip spaces */
            while (p_index < path.Length && path[p_index] == ' ')
            {
                p_index++;
            }

            /* get the third part. */
            val = 0;
            while (p_index < path.Length && isdigit(path[p_index]) && (addr_seg_len < (Session.MAX_IP_ADDR_SEG_LEN - 1)))
            {
                val = (val * 10) + (path[p_index] - '0');
                conn_path[c_index] = (byte)path[p_index];
                c_index++;
                p_index++;
                (addr_seg_len)++;
            }

            if (val < 0 || val > 255)
            {
                //pdebug(DEBUG_WARN, "Third IP address part is out of bounds (0 <= %d < 256) for an IPv4 octet.", val);
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            //pdebug(DEBUG_DETAIL, "Third IP segment: %d.", val);

            /* skip spaces */
            while (p_index < path.Length && path[p_index] == ' ')
            {
                p_index++;
            }

            /* is the next character a dot? */
            if (!(p_index < path.Length) || path[p_index] != '.')
            {
                //pdebug(DEBUG_DETAIL, "Unexpected character '%c' found at position %d in third IP address part.", path[p_index], p_index);
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            /* copy the dot. */
            conn_path[c_index] = (byte)path[p_index];
            c_index++;
            p_index++;
            (addr_seg_len)++;

            /* skip spaces */
            while (path[p_index] == ' ')
            {
                p_index++;
            }

            /* get the fourth part. */
            val = 0;
            while (p_index < path.Length && isdigit(path[p_index]) && (addr_seg_len < (Session.MAX_IP_ADDR_SEG_LEN - 1)))
            {
                val = (val * 10) + (path[p_index] - '0');
                conn_path[c_index] = (byte)path[p_index];
                c_index++;
                p_index++;
                (addr_seg_len)++;
            }

            if (val < 0 || val > 255)
            {
                //pdebug(DEBUG_WARN, "Fourth IP address part is out of bounds (0 <= %d < 256) for an IPv4 octet.", val);
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            //pdebug(DEBUG_DETAIL, "Fourth IP segment: %d.", val);

            /* We need to zero pad if the length is not a multiple of two. */
            if ((addr_seg_len & 0x01) == 1)
            {
                conn_path[c_index] = 0;
                c_index++;
            }

            /* skip spaces */
            while (p_index < path.Length && path[p_index] == ' ')
            {
                p_index++;
            }

            /* set the return values. */
            path_index = p_index;
            conn_path_index = c_index;
            conn_path[addr_seg_len_pos] = addr_seg_len;

            //pdebug(DEBUG_DETAIL, "Done.");

            return PlcTag.PLCTAG_STATUS_OK;
        }

        /*
         * match DH+ address segments.
         *  A:1:2 - port 2/A -> DH+ node 2
         *  B:1:2 - port 3/B -> DH+ node 2
         *
         * A and B can be lowercase or numeric.
         */

        public static int match_dhp_addr_segment(String path, ref int /*size_t¨*/ path_index, ref byte port, ref byte src_node, ref byte dest_node)
        {
            int val = 0;
            int /*size_t*/ p_index = path_index;

            //pdebug(DEBUG_DETAIL, "Starting at position %d in string %s.", (int)(ssize_t)* path_index, path);

            /* Get the port part. */
            switch(path[p_index]) {
                case 'A':
                    /* fall through */
                case 'a':
                    /* fall through */
                case '2':
                    port = 1;
                    break;

                case 'B':
                    /* fall through */
                case 'b':
                    /* fall through */
                case '3':
                    port = 2;
                    break;

                default:
                    //pdebug(DEBUG_DETAIL, "Character '%c' at position %d does not match start of DH+ segment.", path[p_index], (int)(ssize_t) p_index);
                    return PlcTag.PLCTAG_ERR_NOT_FOUND;
                    break;
            }

            p_index++;

            /* skip spaces */
            while(p_index < path.Length && path[p_index] == ' ') {
                p_index++;
            }

            /* is the next character a colon? */
            if (!(p_index < path.Length) || path[p_index] != ':')
            {
                //pdebug(DEBUG_DETAIL, "Character '%c' at position %d does not match first colon expected in DH+ segment.", path[p_index], (int)(ssize_t)p_index);
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            p_index++;

            /* skip spaces */
            while (p_index < path.Length && path[p_index] == ' ')
            {
                p_index++;
            }

            /* get the source node */
            val = 0;
            while (p_index < path.Length && isdigit(path[p_index]))
            {
                val = (val * 10) + (path[p_index] - '0');
                p_index++;
            }

            /* is the source node a valid number? */
            if (val < 0 || val > 255)
            {
                //pdebug(DEBUG_WARN, "Source node DH+ address part is out of bounds (0 <= %d < 256).", val);
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            src_node = (byte) val;

            /* skip spaces */
            while (p_index < path.Length && path[p_index] == ' ')
            {
                p_index++;
            }

            /* is the next character a colon? */
            if (!(p_index < path.Length) || path[p_index] != ':')
            {
                //pdebug(DEBUG_DETAIL, "Character '%c' at position %d does not match the second colon expected in DH+ segment.", path[p_index], (int)(ssize_t)p_index);
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            p_index++;

            /* skip spaces */
            while (p_index < path.Length && path[p_index] == ' ')
            {
                p_index++;
            }

            /* get the destination node */
            val = 0;
            while (p_index < path.Length && isdigit(path[p_index]))
            {
                val = (val * 10) + (path[p_index] - '0');
                p_index++;
            }   

            /* is the destination node a valid number? */
            if (val < 0 || val > 255)
            {
                //pdebug(DEBUG_WARN, "Destination node DH+ address part is out of bounds (0 <= %d < 256).", val);
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            /* skip spaces */
            while (p_index < path.Length && path[p_index] == ' ')
            {
                p_index++;
            }

            dest_node = (byte)val;
            path_index = p_index;

            //pdebug(DEBUG_DETAIL, "Found DH+ path port:%d, source node:%d, destination node:%d.", (int)(unsigned int) * port, (int)(unsigned int)*src_node, (int)(unsigned int)*dest_node);

            //pdebug(DEBUG_DETAIL, "Done.");

            return PlcTag.PLCTAG_STATUS_OK;
        }


        public static int __cip_encode_tag_name(AbTag tag, System.String name)
        {
            System.String name2 = name;
            int rc = PlcTag.PLCTAG_STATUS_OK;
            int encoded_index = 0;
            int name_index = 0;
            bool x = name_index < name.Length;
            int name_len = name.Length;

            if (parse_symbolic_segment(tag, name, ref encoded_index, ref name_index) != PlcTag.PLCTAG_STATUS_OK)
            {
            }

            while (name_index < name_len && encoded_index < MAX_TAG_NAME)
            {
                if (name.Substring(name_index, 1) == ".") //(name[name_index] == '.')
                {
                    name_index++;
                    if (parse_symbolic_segment(tag, name, ref encoded_index, ref name_index) != PlcTag.PLCTAG_STATUS_OK)
                    {

                        if (true) 
                        {
                            break;
                        }
                        else
                        {
                            return PlcTag.PLCTAG_ERR_BAD_PARAM;
                        }
                    }
                    else
                    {
                    }
                }
                else if (true) 
                {
                    
                }
                else
                {
                    break;
                }
            }

            if (name_index != name_len)
            {
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            tag.encoded_name[0] = (byte)((encoded_index - 1) / 2);
            return PlcTag.PLCTAG_STATUS_OK;
        }

        public class AbTag_
        {
            public byte[] encoded_name = new byte[100/*CIP.MAX_TAG_NAME*/];
        }
        public static int cip_encode_tag_name__(/*AbTag tag, System.String name*/)
        {
            AbTag_ tag = new AbTag_();
            System.String name = "";
            System.String name2 = name;
            int rc = PlcTag.PLCTAG_STATUS_OK;
            int encoded_index = 0;
            int name_index = 0;
            bool x = name_index < name.Length;
            int name_len = name.Length;
            bool b = name_index < name_len && encoded_index < MAX_TAG_NAME;
            bool b2 = name.Substring(name_index, 1) == ".";
            //        if (true) //name.Substring(name_index, 1) == ".") 
            //        {
            //        name_index++;
            /*         if (true) //Y(/*tag, name, ref encoded_index, ref name_index*//*) != PlcTag.PLCTAG_STATUS_OK)
                     {

                     }
                     else
                     {
                     }
            *///     }
              //       else if (true) 
              //       {
              //       }
              //       else
              //       {
              //       }
            /*       if (name_index != name_len)
                   {
                   }
            */
            bool b3 = name_index != name_len;
            tag.encoded_name[0] = (byte)((encoded_index - 1) / 2);
            return PlcTag.PLCTAG_STATUS_OK;
        }

        public static int cip_encode_tag_name(AbTag tag, String name)
        {
            //          String name  = "name";
            //          AbTag tag = new AbTag();
            //String name = name_;
            int rc = PlcTag.PLCTAG_STATUS_OK;
            int encoded_index = 0;
            int name_index = 0;
            int name_len = name.Length;

            /* zero out the CIP encoded name size. Byte zero in the encoded name. */
            tag.encoded_name[encoded_index] = (byte)0;
            encoded_index++;

            /* names must start with a symbolic segment. */
            if(parse_symbolic_segment(tag, name,ref encoded_index, ref name_index) != PlcTag.PLCTAG_STATUS_OK) {
                //pdebug(DEBUG_WARN,"Unable to parse initial symbolic segment in tag name %s!", name);
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            while(name_index<name_len && encoded_index<MAX_TAG_NAME) {
                /* try to parse the different parts of the name. */
                if (name.Substring(name_index,1) == ".") //(name[name_index] == '.')
                {
                    name_index++;
                    /* could be a name segment or could be a bit identifier. */
                    if (parse_symbolic_segment(tag, name, ref encoded_index, ref name_index) != PlcTag.PLCTAG_STATUS_OK) {
                
                    /* try a bit identifier. */
                        if(parse_bit_segment(tag, name, ref name_index) == PlcTag.PLCTAG_STATUS_OK) {
                            //pdebug(DEBUG_DETAIL, "Found bit identifier %u.", tag->bit);
                            break;
                        } else
                        {
                        //pdebug(DEBUG_WARN, "Expected a symbolic segment or a bit identifier at position %d in tag name %s", name_index, name);
                        return PlcTag.PLCTAG_ERR_BAD_PARAM;
                        }
                    } else
                    {
                        //pdebug(DEBUG_DETAIL, "Found symbolic segment ending at %d", name_index);
                    }
                } else if (name.Substring(name_index,1) =="[") //(name[name_index] == '[')
                {
                    int num_dimensions = 0;
                    /* must be an array so look for comma separated numeric segments. */
                    //String name_ = name;
                    do
                    {
                        name_index++;
                        num_dimensions++;

                        skip_whitespace(name, ref name_index);
                        rc = parse_numeric_segment(tag, name, ref encoded_index, ref name_index);
                        skip_whitespace(name, ref name_index);
                    } while (rc == PlcTag.PLCTAG_STATUS_OK && name[name_index] == ',' && num_dimensions < 3);

                    /* must terminate with a closing ']' */
                    if (name_index < ((String)name).Length && ((String)name).Substring(name_index,1) != "]")
                    {
                        //pdebug(DEBUG_WARN, "Bad tag name format, expected closing array bracket at %d in tag name %s!", name_index, name);
                        return PlcTag.PLCTAG_ERR_BAD_PARAM;
                    }

                    /* step past the closing bracket. */
                    name_index++;
                }
                else
                {
                    //pdebug(DEBUG_WARN, "Unexpected character at position %d in name string %s!", name_index, name);
                    break;
                }
            }

            if (name_index != name_len)
            {
                //pdebug(DEBUG_WARN, "Bad tag name format.  Tag must end with a bit identifier if one is present.");
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            /* set the word count. */
            tag.encoded_name[0] = (byte)((encoded_index - 1) / 2);
            tag.encoded_name_size = encoded_index;

            return PlcTag.PLCTAG_STATUS_OK;
        }

        public static int skip_whitespace(String name, ref int name_index)
        {
            while (name[name_index] == ' ')
            {
                (name_index)++;
            }

            return PlcTag.PLCTAG_STATUS_OK;
        }

        public static int parse_symbolic_segment(AbTag tag, String name, ref int encoded_index, ref int name_index)
        {
            int encoded_i = encoded_index;
            int name_i = name_index;
            int name_start = name_i;
            int seg_len_index = 0;
            int seg_len = 0;

            //pdebug(DEBUG_DETAIL, "Starting with name index=%d and encoded name index=%d.", name_i, encoded_i);

            /* a symbolic segment must start with an alphabetic character or @, then can have digits or underscores. */
            if (name == null || name_i >= name.ToUpper().Length || name_i >= name.Length ||
                ((name.ToUpper()[name_i] < 'A' || name.ToUpper()[name_i] > 'Z') &&
                name[name_i] != ':' && name[name_i] != '_' && name[name_i] != '@'))
            {
                //pdebug(DEBUG_DETAIL, "tag name at position %d is not the start of a symbolic segment.", name_i);
                return PlcTag.PLCTAG_ERR_NO_MATCH;
            }
 
            /* start building the encoded symbolic segment. */
            tag.encoded_name[encoded_i] = 0x91; /* start of symbolic segment. */
            encoded_i++;
            seg_len_index = encoded_i;
            tag.encoded_name[seg_len_index]++;
            encoded_i++;

            /* store the first character of the name. */
            if (name_i < name.Length)
            {
                tag.encoded_name[encoded_i] = (byte)name[name_i];
            }
            encoded_i++;
            name_i++;

            /* get the rest of the name. */
            while (/*(isalnum(name[name_i])*/
                (name_i < name.Length && name_i < name.ToUpper().Length &&
                ((name.ToUpper()[name_i] >= 'A' && name.ToUpper()[name_i] <= 'Z') ||
                (name[name_i] >= '0' && name[name_i] <= '9') ||
                name[name_i] == ':' || name[name_i] == '_') && (encoded_i < (MAX_TAG_NAME - 1))))
            {
                tag.encoded_name[encoded_i] = (byte)name[name_i];
                encoded_i++;
                tag.encoded_name[seg_len_index]++;
                name_i++;
            }
 
            seg_len = tag.encoded_name[seg_len_index];

            /* finish up the encoded name.   Space for the name must be a multiple of two bytes long. */
            if (((tag.encoded_name[seg_len_index] & 0x01) != 0) && (encoded_i < MAX_TAG_NAME))
            {
                tag.encoded_name[encoded_i] = 0;
                encoded_i++;
            }

            encoded_index = encoded_i;
            name_index = name_i;

            //pdebug(DEBUG_DETAIL, "Parsed symbolic segment \"%.*s\" in tag name.", seg_len, &name[name_start]);
  /**/
            return PlcTag.PLCTAG_STATUS_OK;
        }

        public static int _parse_symbolic_segment(AbTag tag, String name, ref int encoded_index, ref int name_index)
        {
            int encoded_i = encoded_index;
            int name_i = name_index;
            int name_start = name_i;
            int seg_len_index = 0;
            int seg_len = 0;

            //pdebug(DEBUG_DETAIL, "Starting with name index=%d and encoded name index=%d.", name_i, encoded_i);

            /* a symbolic segment must start with an alphabetic character or @, then can have digits or underscores. */
            if (name ==null || ((name.ToUpper()[name_i]<'A' || name.ToUpper()[name_i] >'Z') &&
                name[name_i] != ':' && name[name_i] != '_' && name[name_i] != '@'))
            {
                //pdebug(DEBUG_DETAIL, "tag name at position %d is not the start of a symbolic segment.", name_i);
                return PlcTag.PLCTAG_ERR_NO_MATCH;
            }

            /* start building the encoded symbolic segment. */
            tag.encoded_name[encoded_i] = 0x91; /* start of symbolic segment. */
            encoded_i++;
            seg_len_index = encoded_i;
            tag.encoded_name[seg_len_index]++;
            encoded_i++;

            /* store the first character of the name. */
            tag.encoded_name[encoded_i] = (byte)name[name_i];
            encoded_i++;
            name_i++;

            /* get the rest of the name. */
            while (/*(isalnum(name[name_i])*/
                (name_i<name.Length &&
                ((name.ToUpper()[name_i] >= 'A' && name.ToUpper()[name_i] <='Z') ||
                name[name_i] == ':' || name[name_i] == '_') && (encoded_i < (MAX_TAG_NAME - 1))))
            {
                tag.encoded_name[encoded_i] = (byte)name[name_i];
                encoded_i++;
                tag.encoded_name[seg_len_index]++;
                name_i++;
            }

            seg_len = tag.encoded_name[seg_len_index];

            /* finish up the encoded name.   Space for the name must be a multiple of two bytes long. */
            if (((tag.encoded_name[seg_len_index] & 0x01) != 0) && (encoded_i < MAX_TAG_NAME))
            {
                tag.encoded_name[encoded_i] = 0;
                encoded_i++;
            }

            encoded_index = encoded_i;
            name_index = name_i;

            //pdebug(DEBUG_DETAIL, "Parsed symbolic segment \"%.*s\" in tag name.", seg_len, &name[name_start]);
 
            return PlcTag.PLCTAG_STATUS_OK;
        }

        /*
         * A bit segment is simply an integer from 0 to 63 (inclusive). */
        public static int parse_bit_segment(AbTag tag, String name,ref  int name_index)
        {
            //const char* p, *q;
            int p, q;
            long val;

            //pdebug(DEBUG_DETAIL, "Starting with name index=%d.", * name_index);

            p = name_index; //&name[*name_index];
            q = p;

            val = 0;
 //*HR*           val = strtol((char*) p, (char**)&q, 10);

            /* sanity checks. */
            if(p == q) {
                /* no number. */
                //pdebug(DEBUG_WARN,"Expected bit identifier or symbolic segment at position %d in tag name %s!", * name_index, name);
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            if((val< 0) || (val >= 65536)) {
                //pdebug(DEBUG_WARN,"Bit identifier must be between 0 and 255, inclusive, was %d!", (int) val);
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            if (tag.elem_count != 1)
            {
                //pdebug(DEBUG_WARN, "Bit tags must have only one element!");
                return PlcTag.PLCTAG_ERR_BAD_PARAM;
            }

            /* bump name_index. */
            name_index += (int)(q - p);
            tag.is_bit = true;
            tag.bit = (short)val;

            return PlcTag.PLCTAG_STATUS_OK;
        }

        static int parse_numeric_segment(AbTag tag, String name, ref int encoded_index, ref int name_index)
        {
            //const char* p, *q;
            int p, q;
            long val;

            //pdebug(DEBUG_DETAIL, "Starting with name index=%d and encoded name index=%d.", * name_index, * encoded_index);

            p = name_index; // &name[*name_index];
            q = p;

            val = 0;
  //*HR*          val = strtol((char*) p, (char**)&q, 10);

            /* sanity checks. */
            if(p == q) {
                /* no number. */
            //pdebug(DEBUG_WARN,"Expected numeric segment at position %d in tag name %s!", * name_index, name);
            return PlcTag.PLCTAG_ERR_BAD_PARAM;
        }

        if(val< 0) {
            //pdebug(DEBUG_WARN,"Numeric segment must be greater than or equal to zero, was %d!", (int) val);
            return PlcTag.PLCTAG_ERR_BAD_PARAM;
        }

        /* bump name_index. */
        name_index += (int)(q - p);

        /* encode the segment. */
        if (val > 0xFFFF)
        {
            tag.encoded_name[encoded_index] = (byte)0x2A; /* 4-byte segment value. */
            (encoded_index)++;

            tag.encoded_name[encoded_index] = (byte)0; /* padding. */
            (encoded_index)++;

            tag.encoded_name[encoded_index] = (byte)(val & 0xFF);
            (encoded_index)++;
            tag.encoded_name[encoded_index] = (byte)((val >> 8) & 0xFF);
            (encoded_index)++;
            tag.encoded_name[encoded_index] = (byte)((val >> 16) & 0xFF);
            (encoded_index)++;
            tag.encoded_name[encoded_index] = (byte)((val >> 24) & 0xFF);
            (encoded_index)++;

            //pdebug(DEBUG_DETAIL, "Parsed 4-byte numeric segment of value %u.", (uint32_t)val);
        }
        else if (val > 0xFF)
        {
            tag.encoded_name[encoded_index] = (byte)0x29; /* 2-byte segment value. */
            (encoded_index)++;

            tag.encoded_name[encoded_index] = (byte)0; /* padding. */
            (encoded_index)++;

            tag.encoded_name[encoded_index] = (byte)(val & 0xFF);
            (encoded_index)++;
            tag.encoded_name[encoded_index] = (byte)((val >> 8) & 0xFF);
            (encoded_index)++;

            //pdebug(DEBUG_DETAIL, "Parsed 2-byte numeric segment of value %u.", (uint32_t)val);
        }
        else
        {
            tag.encoded_name[encoded_index] = (byte)0x28; /* 1-byte segment value. */
            (encoded_index)++;

            tag.encoded_name[encoded_index] = (byte)(val & 0xFF);
            (encoded_index)++;

            //pdebug(DEBUG_DETAIL, "Parsed 1-byte numeric segment of value %u.", (uint32_t)val);
        }

        //pdebug(DEBUG_DETAIL, "Done with name index=%d and encoded name index=%d.", *name_index, *encoded_index);

        return PlcTag.PLCTAG_STATUS_OK;
}


        class cip_type_lookup_entry_t
        {
            public cip_type_lookup_entry_t(int is_found, int type_data_length, int instance_data_length)
            {
                this.is_found = is_found;
                this.type_data_length = type_data_length;
                this.instance_data_length= instance_data_length;
            }
            public int is_found;
            public int type_data_length;
            public int instance_data_length;
        };

        public static int cip_lookup_encoded_type_size(byte type_byte, ref int type_size)
        {
            type_size = cip_type_lookup[type_byte].type_data_length;
            return cip_type_lookup[type_byte].is_found;
        }

        static cip_type_lookup_entry_t[] cip_type_lookup = new cip_type_lookup_entry_t[] {
    /* 0x00 */ new cip_type_lookup_entry_t (Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x01 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x02 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x03 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x04 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 2 ),   /* UINT_BCD: OMRON-specific */
    /* 0x05 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 4 ),   /* UDINT_BCD: OMRON-specific */
    /* 0x06 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 8 ),   /* ULINT_BCD: OMRON-specific */
    /* 0x07 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 4 ),   /* ENUM: OMRON-specific */
    /* 0x08 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 8 ),   /* DATE_NSEC: OMRON-specific */
    /* 0x09 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 8 ),   /* TIME_NSEC: OMRON-specific, Time in nanoseconds */
    /* 0x0a */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 8 ),   /* DATE_AND_TIME_NSEC: OMRON-specific, Date/Time in nanoseconds*/
    /* 0x0b */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 8 ),   /* TIME_OF_DAY_NSEC: OMRON-specific */
    /* 0x0c */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),   /* ???? UNION: Omron-specific */
    /* 0x0d */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x0e */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x0f */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x10 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x11 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x12 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x13 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x14 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x15 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x16 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x17 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x18 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x19 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x1a */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x1b */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x1c */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x1d */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x1e */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x1f */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x20 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x21 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x22 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x23 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x24 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x25 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x26 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x27 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x28 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x29 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x2a */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x2b */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x2c */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x2d */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x2e */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x2f */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x30 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x31 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x32 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x33 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x34 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x35 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x36 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x37 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x38 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x39 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x3a */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x3b */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x3c */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x3d */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x3e */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x3f */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x40 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x41 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x42 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x43 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x44 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x45 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x46 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x47 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x48 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x49 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x4a */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x4b */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x4c */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x4d */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x4e */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x4f */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x50 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x51 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x52 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x53 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x54 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x55 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x56 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x57 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x58 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x59 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x5a */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x5b */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x5c */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x5d */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x5e */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x5f */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x60 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x61 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x62 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x63 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x64 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x65 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x66 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x67 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x68 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x69 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x6a */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x6b */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x6c */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x6d */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x6e */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x6f */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x70 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x71 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x72 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x73 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x74 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x75 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x76 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x77 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x78 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x79 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x7a */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x7b */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x7c */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x7d */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x7e */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x7f */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x80 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x81 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x82 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x83 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x84 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x85 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x86 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x87 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x88 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x89 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x8a */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x8b */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x8c */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x8d */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x8e */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x8f */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x90 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x91 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x92 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x93 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x94 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x95 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x96 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x97 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x98 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x99 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x9a */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x9b */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x9c */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x9d */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0x9e */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0x9f */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xa0 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK, 4, 0),   /* Data is an abbreviated struct type, i.e. a CRC of the actual type descriptor */
    /* 0xa1 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK, 4, 0),   /* Data is an abbreviated array type. The limits are left off */
    /* 0xa2 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),   /* Data is a struct type descriptor, marked no match because we do not know how to parse it */
    /* 0xa3 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),   /* Data is an array type descriptor, marked no match because we do not know how to parse it */
    /* 0xa4 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0xa5 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0xa6 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0xa7 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0xa8 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0xa9 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xaa */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xab */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0xac */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0xad */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0xae */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xaf */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xb0 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xb1 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xb2 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xb3 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xb4 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xb5 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xb6 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xb7 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0),
    /* 0xb8 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xb9 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xba */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xbb */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xbc */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xbd */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xbe */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xbf */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xc0 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 8 ),   /* DT: DT value, 64 bit */
    /* 0xc1 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 1 ),   /* BOOL: Boolean value, 1 bit */
    /* 0xc2 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 1 ),   /* SINT: Signed 8–bit integer value */
    /* 0xc3 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 2 ),   /* INT: Signed 16–bit integer value */
    /* 0xc4 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 4 ),   /* DINT: Signed 32–bit integer value */
    /* 0xc5 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 8 ),   /* LINT: Signed 64–bit integer value */
    /* 0xc6 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK, 2, 1),   /* USINT: Unsigned 8–bit integer value */
    /* 0xc7 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK, 2, 2),   /* UINT: Unsigned 16–bit integer value */
    /* 0xc8 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK, 2, 4),   /* UDINT: Unsigned 32–bit integer value */
    /* 0xc9 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK, 2, 8),   /* ULINT: Unsigned 64–bit integer value */
    /* 0xca */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK, 2, 4),   /* REAL: 32–bit floating point value, IEEE format */
    /* 0xcb */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK, 2, 8),   /* LREAL: 64–bit floating point value, IEEE format */
    /* 0xcc */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK, 2, 4),   /* STIME: System Time Synchronous time value */
    /* 0xcd */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK, 2, 2),   /* DATE: Date value */
    /* 0xce */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK, 2, 4),   /* TIME_OF_DAY: Time of day value */
    /* 0xcf */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK, 2, 8),   /* DATE_AND_TIME: Date and time of day value */
    /* 0xd0 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 84),   /* STRING: Character string, 2 byte count word, 1 byte per character */
    /* 0xd1 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 1 ),   /* BYTE: 8-bit bit string */
    /* 0xd2 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 2 ),   /* WORD: 16-bit bit string */
    /* 0xd3 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 4 ),   /* DWORD: 32-bit bit string */
    /* 0xd4 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 8 ),   /* LWORD: 64-bit bit string */
    /* 0xd5 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 0 ),   /* STRING2: Wide string, 2-byte count, 2 bytes per character, utf-16-le */
    /* 0xd6 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 4 ),   /* FTIME: High resolution duration value */
    /* 0xd7 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 8 ),   /* TIME: Medium resolution duration value */
    /* 0xd8 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 2 ),   /* ITIME: Low resolution duration value */
    /* 0xd9 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 0 ),   /* STRINGN: N-byte per char character string */
    /* 0xda */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 0 ),   /* SHORT_STRING: 1 byte per character and 1 byte length */
    /* 0xdb */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 4 ),   /* TIME: Duration in milliseconds */
    /* 0xdc */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 0 ),   /* EPATH: CIP path segment(s) */
    /* 0xdd */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 2 ),   /* ENGUNIT: Engineering units */
    /* 0xde */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_STATUS_OK,    2, 0 ),   /* STRINGI: International character string (encoding?) */
    /* 0xdf */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xe0 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xe1 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xe2 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xe3 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xe4 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xe5 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xe6 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xe7 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xe8 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xe9 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xea */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xeb */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xec */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xed */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xee */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xef */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xf0 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xf1 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xf2 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xf3 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xf4 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xf5 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xf6 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xf7 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xf8 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xf9 */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xfa */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xfb */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xfc */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xfd */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xfe */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 ),
    /* 0xff */ new cip_type_lookup_entry_t(Lib.PlcTag.PLCTAG_ERR_NO_MATCH, 0, 0 )
                   };

    }
}
