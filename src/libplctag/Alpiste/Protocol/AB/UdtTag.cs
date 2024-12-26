using Alpiste.Utils;
using libplctag.NativeImport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Alpiste.Protocol.AB
{
    internal class UdtTag : AbTag
    {
        /* define the vtable for udt tag type. */
        //struct tag_vtable_t udt_tag_vtable = {
        //    (tag_vtable_func)ab_tag_abort, /* shared */
        //    (tag_vtable_func)udt_tag_read_start,
        //    (tag_vtable_func)ab_tag_status, /* shared */
        //    (tag_vtable_func)udt_tag_tickler,
        //    (tag_vtable_func)NULL, /* write */
        //    (tag_vtable_func)NULL, /* wake_plc */
        //
        //    /* attribute accessors */
        //    ab_get_int_attrib,
        //    ab_set_int_attrib,
        //
        //    ab_get_byte_array_attrib
        //};

        //override public int abort() { return 0; }
        override public int read() { return udt_tag_read_start(); }
        //override public int status() { return ab_tag_status(); }
        override public int tickler() { return udt_tag_tickler(); }
        override public int write() { return 0; }

        override public int wake_plc() { return 0; }


        public class udt_tag_logix_byte_order : Alpiste.Lib.tag_byte_order_t
        {
            public udt_tag_logix_byte_order()
            {

                is_allocated = false;

                int16_order = new Int16[] { 0, 1 };
                int32_order = new Int16[] { 0, 1, 2, 3 };
                int64_order = new Int16[] { 0, 1, 2, 3, 4, 5, 6, 7 };
                float32_order = new short[] { 0, 1, 2, 3 };
                float64_order = new Int16[] { 0, 1, 2, 3, 4, 5, 6, 7 };
                str_is_defined = true;
                str_is_counted = false;
                str_is_fixed_length = false;
                str_is_zero_terminated = true;
                str_is_byte_swapped = false;

                str_pad_to_multiple_bytes = 1;
                str_count_word_bytes = 0;
                str_max_capacity = 0;
                str_total_length = 0;
                str_pad_bytes = 0;
            }

        };

        public UdtTag(int tag_id, attr attribs, plctag.callback_func_ex tag_callback_func = null, object userdata = null) : base(attribs, tag_callback_func, userdata)
        {
            this.udt_id = (UInt16) tag_id;
            special_tag = 1;
            elem_type = Lib.ElemType.AB_TYPE_TAG_UDT;
            elem_count = 1;
            elem_size = 1;

            byte_order = new udt_tag_logix_byte_order();

        }

        /*
 * udt_tag_read_start
 *
 * This function must be called only from within one thread, or while
 * the tag's mutex is locked.
 *
 * The function starts the process of getting UDT data from the PLC.
 */

        int udt_tag_read_start(/*ab_tag_p tag*/)
        {
            int rc = PLCTAG_STATUS_OK;

            //pdebug(DEBUG_INFO, "Starting");

            if (write_in_progress != 0)
            {
                //pdebug(DEBUG_WARN, "A write is in progress on a UDT tag!");
                return PLCTAG_ERR_BAD_STATUS;
            }

            if (read_in_progress != 0)
            {
                //pdebug(DEBUG_WARN, "Read or write operation already in flight!");
                return PLCTAG_ERR_BUSY;
            }

            /* mark the tag read in progress */
            read_in_progress = 1;

            /* set up the state for the requests (there are two!) */
            udt_get_fields = 0;
            offset = 0;

            /* build the new request */
            rc = udt_tag_build_read_metadata_request_connected(/*tag*/);
            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_WARN, "Unable to build read request!");

                read_in_progress = 0;

                return rc;
            }

            //pdebug(DEBUG_INFO, "Done.");

            return PLCTAG_STATUS_PENDING;
        }



        int udt_tag_tickler(/*ab_tag_p tag*/)
        {
            int rc = PLCTAG_STATUS_OK;

            //pdebug(DEBUG_SPEW, "Starting.");

            if (read_in_progress != 0)
            {
                if (elem_type == Lib.ElemType.AB_TYPE_TAG_RAW)
                {
                    //pdebug(DEBUG_WARN, "Something started a read on a raw tag.  This is not supported!");
                    read_in_progress = 0;
                    read_in_flight = false;
                }

                if (udt_get_fields != 0)
                {
                    rc = udt_tag_check_read_fields_status_connected(/*tag*/);
                }
                else
                {
                    rc = udt_tag_check_read_metadata_status_connected(/*tag*/);
                }

                status_ = rc;

                /* if the operation completed, make a note so that the callback will be called. */
                if (read_in_progress == 0)
                {
                    //pdebug(DEBUG_DETAIL, "Read complete.");
                    read_complete = true;
                }

                //pdebug(DEBUG_SPEW, "Done.  Read in progress.");

                return rc;
            }

            //pdebug(DEBUG_SPEW, "Done.  No operation in progress.");

            return status_;
        }


        int udt_tag_build_read_metadata_request_connected(/*ab_tag_p tag*/)
        {
            eip_cip_co_req cip = null;
            //tag_list_req *list_req = NULL;
            Request /*ab_request_p*/ req = null;
            int rc = PLCTAG_STATUS_OK;
            int data_start = 0; //NULL;
            int /*uint8_t**/ data = 0; // NULL;
            UInt16 /*uint16_le*/ tmp_u16 = 0; // UINT16_LE_INIT(0);

            //pdebug(DEBUG_INFO, "Starting.");

            /* get a request buffer */
            Session session = null;
            sessionRef.TryGetTarget(out session);
            rc = session.session_create_request(/*tag->session, tag->*/ tag_id, ref req);
            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_ERROR, "Unable to get new request.  rc=%d", rc);
                return rc;
            }

            /* point the request struct at the buffer */
            cip = new eip_cip_co_req();

            //cip = (eip_cip_co_req*)(req->data);

            /* point to the end of the struct */
            data_start = data = eip_cip_co_req.BASE_SIZE; // (uint8_t*)(cip + 1);

            /*
             * set up the embedded CIP UDT metadata request packet
                uint8_t request_service;    AB_EIP_CMD_CIP_GET_ATTR_LIST=0x03
                uint8_t request_path_size;  3 word = 6 bytes
                uint8_t request_path[6];        0x20    get class
                                                0x6C    UDT class
                                                0x25    get instance (16-bit)
                                                0x00    padding
                                                0x00    instance byte 0
                                                0x00    instance byte 1
                uint16_le instance_id;      NOTE! this is the last two bytes above for convenience!
                uint16_le num_attributes;   0x04    number of attributes to get
                uint16_le requested_attributes[4];      0x04    attribute #4 - Number of 32-bit words in the template definition.
                                                        0x05    attribute #5 - Number of bytes in the structure on the wire.
                                                        0x02    attribute #2 - Number of structure members.
                                                        0x01    attribute #1 - Handle/type of structure.
            */

            req.data[data] = Defs.AB_EIP_CMD_CIP_GET_ATTR_LIST;
            data++;

            /* request path size, in 16-bit words */
            req.data[data] = 3; // (uint8_t)(3); /* size in words of routing header + routing and instance ID. */
            data++;

            /* add in the routing header . */

            /* first the fixed part. */
            req.data[data] = 0x20; /* class type */
            req.data[data + 1] = 0x6C; /* UDT class */
            req.data[data + 2] = 0x25; /* 16-bit instance ID type */
            req.data[data + 3] = 0x00; /* padding */
            data += 4;

            /* now the instance ID */
            tmp_u16 = /*h2le16((uint16_t)tag->*/udt_id;
            //mem_copy(data, &tmp_u16, (int)sizeof(tmp_u16));
            req.data[data] = (byte)(tmp_u16 & 255);
            req.data[data + 1] = (byte)(tmp_u16 >> 8);
            data += 2; // (int)sizeof(tmp_u16);

            /* set up the request itself.  We are asking for a number of attributes. */

            /* set up the request attributes, first the number of attributes. */
            tmp_u16 = 4; // h2le16((uint16_t)4);  /* MAGIC, we have four attributes we want. */
            //mem_copy(data, &tmp_u16, (int)sizeof(tmp_u16));
            req.data[data] = (byte)(tmp_u16 & 255);
            req.data[data + 1] = (byte)(tmp_u16 >> 8);
            data += 2; // (int)sizeof(tmp_u16);

            /* first attribute: symbol type */
            tmp_u16 = 4; // h2le16((uint16_t)0x04);  /* MAGIC, Total field definition size in 32-bit words. */
            //mem_copy(data, &tmp_u16, (int)sizeof(tmp_u16));
            req.data[data] = (byte)(tmp_u16 & 255);
            req.data[data + 1] = (byte)(tmp_u16 >> 8);
            data += 2; // (int)sizeof(tmp_u16);

            /* second attribute: base type size in bytes */
            tmp_u16 = 5; // h2le16((uint16_t)0x05);  /* MAGIC, struct size in bytes. */
            //mem_copy(data, &tmp_u16, (int)sizeof(tmp_u16));
            req.data[data] = (byte)(tmp_u16 & 255);
            req.data[data + 1] = (byte)(tmp_u16 >> 8);
            data += 2;  //(int)sizeof(tmp_u16);

            /* third attribute: tag array dimensions */
            tmp_u16 = 2;  // h2le16((uint16_t)0x02);  /* MAGIC, number of structure members. */
            //mem_copy(data, &tmp_u16, (int)sizeof(tmp_u16));
            req.data[data] = (byte)(tmp_u16 & 255);
            req.data[data + 1] = (byte)(tmp_u16 >> 8);
            data += 2;  // (int)sizeof(tmp_u16);

            /* fourth attribute: symbol/tag name */
            tmp_u16 = 1;  // h2le16((uint16_t)0x01);  /* MAGIC, struct type/handle. */
            //mem_copy(data, &tmp_u16, (int)sizeof(tmp_u16));
            req.data[data] = (byte)(tmp_u16 & 255);
            req.data[data + 1] = (byte)(tmp_u16 >> 8);
            data += 2;  // (int)sizeof(tmp_u16);

            /* now we go back and fill in the fields of the static part */

            /* encap fields */
            cip.encap_command = /*h2le16(*/Defs.AB_EIP_CONNECTED_SEND; /* ALWAYS 0x0070 Connected Send*/

            /* router timeout */
            cip.router_timeout = 1; // h2le16(1); /* one second timeout, enough? */

            /* Common Packet Format fields for unconnected send. */
            cip.cpf_item_count = 2;  // h2le16(2);                 /* ALWAYS 2 */
            cip.cpf_cai_item_type = /*h2le16(*/Defs.AB_EIP_ITEM_CAI;/* ALWAYS 0x00A1 connected address item */
            cip.cpf_cai_item_length = 4; // h2le16(4);            /* ALWAYS 4, size of connection ID*/
            cip.cpf_cdi_item_type = /*h2le16(*/ Defs.AB_EIP_ITEM_CDI;/* ALWAYS 0x00B1 - connected Data Item */
            cip.cpf_cdi_item_length = /*h2le16((uint16_t)((int)*/(UInt16)(data - data_start + 2); // (int)sizeof(cip.cpf_conn_seq_num)));

            Array.Copy(cip.encodedData(), 0, req.data, 0, eip_cip_co_req.BASE_SIZE);

            /* set the size of the request */
            req.request_size = eip_cip_co_req.BASE_SIZE + /*//(int)((int)sizeof(*cip) +(int)*/(data - data_start);

            req.allow_packing = allow_packing;

            /* add the request to the session's list. */
            rc = session.session_add_request(/*tag->session,*/ req);

            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_ERROR, "Unable to add request to session! rc=%d", rc);
                //tag->req = rc_dec(req);
                return rc;
            }

            /* save the request for later */
            this.req = new WeakReference<Request>(req);

            //pdebug(DEBUG_INFO, "Done");

            return PLCTAG_STATUS_OK;
        }

        /*
     * udt_tag_check_read_fields_status_connected
     *
     * This routine checks for any outstanding tag udt field data requests.  It will
     * terminate when there is no data in the response and the error is not "more data".
     *
     * This is not thread-safe!  It should be called with the tag mutex
     * locked!
     */

        int udt_tag_check_read_fields_status_connected(/*ab_tag_p tag */)
        {
            int rc = PLCTAG_STATUS_OK;
            eip_cip_co_resp cip_resp;
            int /*uint8_t**/ data;
            int /*uint8_t**/ data_end;
            int partial_data = 0;
            Request /*ab_request_p*/ request = null;

            //pdebug(DEBUG_SPEW, "Starting.");

            /*if (!tag)
            {
                pdebug(DEBUG_ERROR, "Null tag pointer passed!");
                return PLCTAG_ERR_NULL_PTR;
            }*/

            /* guard against the request being deleted out from underneath us. */
            request = new Request(); // rc_inc(tag->req);
            rc = check_read_request_status(/*tag,*/ request);
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
            data = eip_cip_co_resp.base_size; // (request->data) + sizeof(eip_cip_co_resp);

            /* point the end of the data */
            data_end = eip_encap.encap_size + cip_resp.encap_length; // (request->data + le2h16(cip_resp->encap_length) + sizeof(eip_encap));

            /* check the status */
            do
            {
                int /*ptrdiff_t*/ payload_size = (data_end - data);

                if (/*le2h16(*/cip_resp.encap_command != Defs.AB_EIP_CONNECTED_SEND)
                {
                    //pdebug(DEBUG_WARN, "Unexpected EIP packet type received: %d!", cip_resp->encap_command);
                    rc = PLCTAG_ERR_BAD_DATA;
                    break;
                }

                if (/*le2h32(*/cip_resp.encap_status != Defs.AB_EIP_OK)
                {
                    //pdebug(DEBUG_WARN, "EIP command failed, response code: %d", le2h32(cip_resp->encap_status));
                    rc = PLCTAG_ERR_REMOTE_ERR;
                    break;
                }

                if (cip_resp.reply_service != (Defs.AB_EIP_CMD_CIP_READ | Defs.AB_EIP_CMD_CIP_OK))
                {
                    //pdebug(DEBUG_WARN, "CIP response reply service unexpected: %d", cip_resp->reply_service);
                    rc = PLCTAG_ERR_BAD_DATA;
                    break;
                }

                if (cip_resp.status != Defs.AB_CIP_STATUS_OK && cip_resp.status != Defs.AB_CIP_STATUS_FRAG)
                {
                    //pdebug(DEBUG_WARN, "CIP read failed with status: 0x%x %s", cip_resp->status, decode_cip_error_short((uint8_t*)&cip_resp->status));
                    //pdebug(DEBUG_INFO, decode_cip_error_long((uint8_t*)&cip_resp->status));
                    //*HR* Ver                 rc = decode_cip_error_code((uint8_t*)&cip_resp->status);
                    rc = PLCTAG_ERR_BAD_STATUS;
                    break;
                }

                /* check to see if this is a partial response. */
                partial_data = (cip_resp.status == Defs.AB_CIP_STATUS_FRAG) ? 1 : 0;

                /*
                 * check to see if there is any data to process.  If this is a packed
                 * response, there might not be.
                 */
                if (payload_size > 0)
                {
                    byte[]/*uint8_t**/ new_buffer = null;
                    int new_size = (int)(size) + (int)payload_size;

                    //pdebug(DEBUG_DETAIL, "Increasing tag buffer size to %d bytes.", new_size);

                    new_buffer = new byte[new_size];
                    //new_buffer = (uint8_t*)mem_realloc(tag->data, new_size);
                    /*if (!new_buffer)
                    {
                        //pdebug(DEBUG_WARN, "Unable to reallocate tag data memory!");
                        rc = PLCTAG_ERR_NO_MEM;
                        break;
                    }*/

                    /* copy the data into the tag's data buffer. */
                    //mem_copy(new_buffer + tag->offset + 14, data, (int)payload_size); /* MAGIC, offset plus the header. */
                    Array.Copy(this.data, 0, new_buffer, offset, payload_size);

                    this.data = new_buffer;
                    size = new_size;
                    elem_size = new_size;

                    offset += (int)payload_size;

                    //pdebug(DEBUG_DETAIL, "payload of %d (%x) bytes resulting in current offset %d", (int)payload_size, (int)payload_size, tag->offset);
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
            //tag.req = rc_dec(request);

            /*
             * huh?  Yes, we do it a second time because we already had
             * a reference and got another at the top of this function.
             * So we need to remove it twice.   Once for the capture above,
             * and once for the original reference.
             */

            //rc_dec(request);

            /* are we actually done? */
            if (rc == PLCTAG_STATUS_OK)
            {
                /* keep going if we are not done yet. */
                if (partial_data != 0)
                {
                    /* call read start again to try again.  The data returned might be zero bytes if this is a packed result */
                    //pdebug(DEBUG_DETAIL, "calling udt_tag_build_read_metadata_request_connected() to try again.");
                    rc = udt_tag_build_read_fields_request_connected();
                }
                else
                {
                    /* done! */
                    //pdebug(DEBUG_DETAIL, "Done reading udt field data.  Tag buffer contains:");
                    //pdebug_dump_bytes(DEBUG_DETAIL, tag->data, tag->size);

                    elem_count = 1;

                    /* this read is done. */
                    udt_get_fields = 0;
                    read_in_progress = 0;
                    offset = 0;
                }
            }

            /* this is not an else clause because the above if could result in bad rc. */
            if (rc != PLCTAG_STATUS_OK && rc != PLCTAG_STATUS_PENDING)
            {
                /* error ! */
                //pdebug(DEBUG_WARN, "Error received: %s!", plc_tag_decode_error(rc));

                offset = 0;
                udt_get_fields = 0;

                /* clean up everything. */
                //ab_tag_abort();
                abort();
            }

            //pdebug(DEBUG_SPEW, "Done.");

            return rc;
        }

        int udt_tag_build_read_fields_request_connected(/*ab_tag_p tag*/)
        {
            eip_cip_co_req cip = null;
            //tag_list_req *list_req = NULL;
            Request /*ab_request_p*/ req = null;
            int rc = PLCTAG_STATUS_OK;
            int /*uint8_t**/ data_start = 0; // NULL;
            int /*uint8_t**/ data = 0;  // NULL;
            UInt16 /*uint16_le*/ tmp_u16 = 0; // UINT16_LE_INIT(0);
            UInt32 /*uint32_le*/ tmp_u32 = 0; // UINT32_LE_INIT(0);
            UInt32 /*uint32_t*/ total_size = 0;
            UInt32 /*uint32_t*/ neg_4 = (~ (UInt32)4) + 1; /* twos-complement */

            //pdebug(DEBUG_INFO, "Starting.");

            /* get a request buffer */
            Session session;
            sessionRef.TryGetTarget(out session);

            rc = session.session_create_request(/*tag->session, tag->*/ tag_id, ref req);
            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_ERROR, "Unable to get new request.  rc=%d", rc);
                return rc;
            }

            /* calculate the total size we need to get. */
            //mem_copy(&tmp_u32, tag->data + 2, (int)(unsigned int)(sizeof(tmp_u32)));
            tmp_u32 = (uint)(this.data[2] + this.data[3] << 8 + this.data[4] << 16 + this.data[5] << 24);

            //total_size = (4 * le2h32(tmp_u32)) - 23; /* formula according to the docs. */
            total_size = (4 * tmp_u32) - 23; /* formula according to the docs. */

            //pdebug(DEBUG_DETAIL, "Calculating total size of request, %d to %d.", (int)(unsigned int)total_size, (int)(unsigned int)((total_size + (uint32_t)3) & (uint32_t)neg_4));

            /* make the total size a multiple of 4 bytes.  Round up. */
            total_size = (total_size + 3) & (UInt32)neg_4;

            /* point the request struct at the buffer */
            //cip = (eip_cip_co_req*)(req->data);
            cip = eip_cip_co_req.createFromData(req.data);

            /* point to the end of the struct */
            data_start = data = 0; // (uint8_t*)(cip + 1);

            /*
             * set up the embedded CIP UDT metadata request packet
                uint8_t request_service;        AB_EIP_CMD_CIP_READ=0x4C
                uint8_t request_path_size;      3 word = 6 bytes
                uint8_t request_path[6];        0x20    get class
                                                0x6C    UDT class
                                                0x25    get instance (16-bit)
                                                0x00    padding
                                                0x00    instance byte 0
                                                0x00    instance byte 1
                uint32_t offset;                Byte offset in ongoing requests.
                uint16_t total_size;            Total size of request in bytes.
            */

            this.data[data] = Defs.AB_EIP_CMD_CIP_READ;
            data++;

            /* request path size, in 16-bit words */
            this.data[data] = 3; // (uint8_t)(3); /* size in words of routing header + routing and instance ID. */
            data++;

            /* add in the routing header . */

            /* first the fixed part. */
            this.data[data] = 0x20; /* class type */
            this.data[data + 1] = 0x6C; /* UDT class */
            this.data[data + 2] = 0x25; /* 16-bit instance ID type */
            this.data[data + 3] = 0x00; /* padding */
            data += 4;

            /* now the instance ID */
            //tmp_u16 = h2le16((uint16_t)tag->udt_id);
            tmp_u16 = udt_id;
            //mem_copy(data, &tmp_u16, (int)sizeof(tmp_u16));
            this.data[data] = (byte) (tmp_u16 & 255);
            this.data[data+1] = (byte) (tmp_u16 >> 8);

            data += 2; // (int)sizeof(tmp_u16);

            /* set the offset */
            //tmp_u32 = h2le32((uint32_t)(tag->offset));
            tmp_u32 = (UInt32) offset;

            //mem_copy(data, &tmp_u32, (int)(unsigned int)sizeof(tmp_u32));
            this.data[data] = (byte)(tmp_u16 & 255);
            this.data[data + 1] = (byte)(tmp_u16 >> 8 & 255);
            this.data[data + 2] = (byte)(tmp_u16 >> 8 & 255);
            this.data[data + 3] = (byte)(tmp_u16 >> 8 & 255);
            data += 4; // sizeof(tmp_u32);

            /* set the total size */
            //pdebug(DEBUG_DETAIL, "Total size %d less offset %d gives %d bytes for the request.", total_size, tag->offset, ((int)(unsigned int)total_size - tag->offset));
            tmp_u16 = (UInt16) (/*h2le16((uint16_t)(*/total_size - /*(uint16_t)(unsigned int)tag->*/offset);
            //mem_copy(data, &tmp_u16, (int)(unsigned int)sizeof(tmp_u16));
            this.data[data] = (byte)(tmp_u16 & 255);
            this.data[data + 1] = (byte)(tmp_u16 >> 8);

            data += 2;  // sizeof(tmp_u16);

            /* now we go back and fill in the fields of the static part */

            /* encap fields */
            cip.encap_command = /*h2le16(*/Defs.AB_EIP_CONNECTED_SEND; /* ALWAYS 0x0070 Connected Send*/

            /* router timeout */
            cip.router_timeout = 1;  // h2le16(1); /* one second timeout, enough? */

            /* Common Packet Format fields for unconnected send. */
            cip.cpf_item_count = 2;  //h2le16(2);                 /* ALWAYS 2 */
            cip.cpf_cai_item_type = /*h2le16(*/ Defs.AB_EIP_ITEM_CAI;/* ALWAYS 0x00A1 connected address item */
            cip.cpf_cai_item_length = 4;  // h2le16(4);            /* ALWAYS 4, size of connection ID*/
            cip.cpf_cdi_item_type = /*h2le16(*/ Defs.AB_EIP_ITEM_CDI;/* ALWAYS 0x00B1 - connected Data Item */
            cip.cpf_cdi_item_length = (UInt16) /*h2le16((uint16_t)((int)(*/(data - data_start + 2); // (int)sizeof(cip.cpf_conn_seq_num)));

            Array.Copy(cip.encodedData(), 0, req.data, 0, eip_cip_co_req.BASE_SIZE);

            /* set the size of the request */
            req.request_size = eip_cip_co_req.BASE_SIZE + data; /*(int)((int)sizeof(*cip) +(int)(data - data_start));

            req->allow_packing = tag->allow_packing;

            /* add the request to the session's list. */
            rc = session.session_add_request(/*tag->session,*/ req);

            if (rc != PLCTAG_STATUS_OK)
            {
                //pdebug(DEBUG_ERROR, "Unable to add request to session! rc=%d", rc);
                //tag->req = rc_dec(req);
                return rc;
            }

            /* save the request for later */
            this.req = new WeakReference<Request> (req);

            //pdebug(DEBUG_INFO, "Done");

            return PLCTAG_STATUS_OK;
        }

        /*
 * udt_tag_check_read_metadata_status_connected
 *
 * This routine checks for any outstanding tag udt requests.  It will
 * terminate when there is no data in the response and the error is not "more data".
 *
 * This is not thread-safe!  It should be called with the tag mutex
 * locked!
 */

        int udt_tag_check_read_metadata_status_connected(/*ab_tag_p tag*/)
        {
            int rc = PLCTAG_STATUS_OK;
            eip_cip_co_resp cip_resp;
            int /*uint8_t**/ data;
            int /*uint8_t**/ data_end;
            int partial_data = 0;
            Request /*ab_request_p*/ request = null;

            //pdebug(DEBUG_SPEW, "Starting.");

            /*if (!tag)
            {
                pdebug(DEBUG_ERROR, "Null tag pointer passed!");
                return PLCTAG_ERR_NULL_PTR;
            }*/

            /* guard against the request being deleted out from underneath us. */
            //request = rc_inc(tag->req);
            rc = check_read_request_status(/*tag,*/ request);
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
            data = eip_cip_co_resp.base_size; // (request.data) + sizeof(eip_cip_co_resp);

            /* point the end of the data */
            data_end = eip_encap.encap_size + cip_resp.encap_length; //(request->data + le2h16(cip_resp->encap_length) + sizeof(eip_encap));

            /* check the status */
            do
            {
                int /*ptrdiff_t*/ payload_size = (data_end - data);

                if (/*le2h16(*/cip_resp.encap_command != Defs.AB_EIP_CONNECTED_SEND)
                {
                    //pdebug(DEBUG_WARN, "Unexpected EIP packet type received: %d!", cip_resp->encap_command);
                    //rc = PLCTAG_ERR_BAD_DATA;
                    break;
                }

                if (/*le2h32(*/cip_resp.encap_status != Defs.AB_EIP_OK)
                {
                    //pdebug(DEBUG_WARN, "EIP command failed, response code: %d", le2h32(cip_resp->encap_status));
                    rc = PLCTAG_ERR_REMOTE_ERR;
                    break;
                }

                if (cip_resp.reply_service != (Defs.AB_EIP_CMD_CIP_GET_ATTR_LIST | Defs.AB_EIP_CMD_CIP_OK))
                {
                    //pdebug(DEBUG_WARN, "CIP response reply service unexpected: %d", cip_resp->reply_service);
                    rc = PLCTAG_ERR_BAD_DATA;
                    break;
                }

                if (cip_resp.status != Defs.AB_CIP_STATUS_OK && cip_resp.status != Defs.AB_CIP_STATUS_FRAG)
                {
                    //pdebug(DEBUG_WARN, "CIP read failed with status: 0x%x %s", cip_resp->status, decode_cip_error_short((uint8_t*)&cip_resp->status));
                    //pdebug(DEBUG_INFO, decode_cip_error_long((uint8_t*)&cip_resp->status));
      //*HR*              rc = decode_cip_error_code((uint8_t*)&cip_resp->status);
                    rc = Defs.AB_CIP_ERR_PARTIAL_ERROR;         
                    break;
                }

                /* check to see if this is a partial response. */
                partial_data = (cip_resp.status == Defs.AB_CIP_STATUS_FRAG)? 1:0;

                /*
                 * check to see if there is any data to process.  If this is a packed
                 * response, there might not be.
                 */
                if (payload_size > 0 && partial_data==0)
                {
                    byte[] /*uint8_t**/ new_buffer = null;
                    int new_size = 14; /* MAGIC, size of the header below */
                    UInt32 /*uint32_le*/ tmp_u32;
                    UInt16 /*uint16_le*/ tmp_u16;
                    byte[] /*uint8_t**/ payload = new byte[payload_size]; /* (uint8_t*)(cip_resp + 1);*/

                    Array.Copy(cip_resp.data, 1, payload, 0, payload_size);

                    /*
                     * We are going to build a 14-byte fake header in the buffer:
                     *
                     * Bytes   Meaning
                     * 0-1     16-bit UDT ID
                     * 2-5     32-bit UDT member description size, in 32-bit words.
                     * 6-9     32-bit UDT instance size, in bytes.
                     * 10-11   16-bit UDT number of members (fields).
                     * 12-13   16-bit UDT handle/type.
                     */

                    //pdebug(DEBUG_DETAIL, "Increasing tag buffer size to %d bytes.", new_size); /* MAGIC */

                    //new_buffer = (uint8_t*)mem_realloc(tag->data, new_size);
                    new_buffer = new byte[new_size]; // (uint8_t*)mem_realloc(tag->data, new_size);
                    Array.Copy(this.data, new_buffer, new_size);

                    /*if (!new_buffer)
                    {
                        pdebug(DEBUG_WARN, "Unable to reallocate tag data memory!");
                        rc = PLCTAG_ERR_NO_MEM;
                        break;
                    }*/

                    this.data = new_buffer;
                    this.size = new_size;
                    this.elem_count = 1;
                    this.elem_size = new_size;

                    /* fill in the data. */

                    /* put in the UDT ID */
                    tmp_u16 = /*h2le16(tag->*/udt_id;

                    //mem_copy(tag->data + 0, &tmp_u16, (int)(unsigned int)(sizeof(tmp_u16)));
                    this.data[0] = (byte)(tmp_u16 & 255);
                    this.data[1] = (byte)(tmp_u16 >> 8);

                    /* copy in the UDT member description size in 32-bit words */
                    //mem_copy(tag->data + 2, payload + 6, (int)(unsigned int)(sizeof(tmp_u32)));
                    this.data[2] = payload[6];
                    this.data[3] = payload[7];
                    this.data[4] = payload[8];
                    this.data[5] = payload[9];

                    /* copy in the UDT instance size in bytes */
                    //mem_copy(tag->data + 6, payload + 14, (int)(unsigned int)(sizeof(tmp_u32)));
                    this.data[6] = payload[14];
                    this.data[7] = payload[15];
                    this.data[8] = payload[16];
                    this.data[9] = payload[17];

                    /* copy in the UDT number of members */
                    //mem_copy(tag->data + 10, payload + 22, (int)(unsigned int)(sizeof(tmp_u16)));
                    this.data[10] = payload[22];
                    this.data[11] = payload[23];
                    
                    /* copy in the UDT number of members */
                    //mem_copy(tag->data + 12, payload + 28, (int)(unsigned int)(sizeof(tmp_u16)));
                    this.data[12] = payload[28];
                    this.data[13] = payload[29];

                    //pdebug(DEBUG_DETAIL, "current size %d", tag->size);
                    //pdebug_dump_bytes(DEBUG_DETAIL, tag->data, tag->size);
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
            //tag->req = rc_dec(request);

            /*
             * huh?  Yes, we do it a second time because we already had
             * a reference and got another at the top of this function.
             * So we need to remove it twice.   Once for the capture above,
             * and once for the original reference.
             */

            //rc_dec(request);

            /* are we actually done? */
            if (rc == PLCTAG_STATUS_OK)
            {
                /* keep going if we are not done yet. */
                if (partial_data !=0 )
                {
                    /* call read start again to try again.  The data returned might be zero bytes if this is a packed result */
                    //pdebug(DEBUG_DETAIL, "calling udt_tag_build_read_metadata_request_connected() to try again.");
                    rc = udt_tag_build_read_metadata_request_connected();
                }
                else
                {
                    /* done! */
                    //pdebug(DEBUG_DETAIL, "Done reading udt metadata!");

                    elem_count = 1;
                    offset = 0;
                    udt_get_fields = 1;

                    //pdebug(DEBUG_DETAIL, "calling udt_tag_build_read_fields_request_connected() to get field data.");
                    rc = udt_tag_build_read_fields_request_connected();
                }
            }

            /* this is not an else clause because the above if could result in bad rc. */
            if (rc != PLCTAG_STATUS_OK && rc != PLCTAG_STATUS_PENDING)
            {
                /* error ! */
                //pdebug(DEBUG_WARN, "Error received: %s!", plc_tag_decode_error(rc));

                offset = 0;
                udt_get_fields = 0;

                /* clean up everything. */
                //ab_tag_abort(tag);
                abort();
            }

            //pdebug(DEBUG_SPEW, "Done.");

            return rc;
        }

    }
}
