using Alpiste.Protocol.AB;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
//using System.Transactions;

namespace Alpiste.Protocol.AB
{

    public class Defs
    {

        public const UInt16 AB_EIP_PLC5_PARAM = (0x4302);
        public const UInt16 AB_EIP_SLC_PARAM = (0x4302);
        public const UInt16 AB_EIP_LGX_PARAM = (0x43F8);
        public const UInt16 AB_EIP_CONN_PARAM = (0x4200);
        //0100 0011 1111 1000
        //0100 001 1 1111 1000
        public const UInt32 AB_EIP_CONN_PARAM_EX = (0x42000000);
        //0100 001 0 0000 0000  0000 0100 0000 0000
        //0x42000400


        public const int DEFAULT_MAX_REQUESTS = (10);   /* number of requests and request sizes to allocate by default. */


        /* AB Constants*/
        public const int AB_EIP_OK = (0);
        public const UInt16 AB_EIP_VERSION = (0x0001);

        /* in milliseconds */
        public const int AB_EIP_DEFAULT_TIMEOUT = 2000; /* in ms */

        /* AB Commands */
        public const UInt16 AB_EIP_REGISTER_SESSION = (0x0065);
        public const UInt16 AB_EIP_UNREGISTER_SESSION = (0x0066);
        public const UInt16 AB_EIP_UNCONNECTED_SEND = (0x006F);
        public const UInt16 AB_EIP_CONNECTED_SEND = (0x0070);

        /* AB packet info */
        public const int AB_EIP_DEFAULT_PORT = 44818;

        /* specific sub-commands */
        public const Byte AB_EIP_CMD_PCCC_EXECUTE = (0x4B);
        public const Byte AB_EIP_CMD_FORWARD_CLOSE = (0x4E);
        public const Byte AB_EIP_CMD_UNCONNECTED_SEND = (0x52);
        public const Byte AB_EIP_CMD_FORWARD_OPEN = (0x54);
        public const Byte AB_EIP_CMD_FORWARD_OPEN_EX = (0x5B);

        /* CIP embedded packet commands */
        public const Byte AB_EIP_CMD_CIP_GET_ATTR_LIST = (0x03);
        public const Byte AB_EIP_CMD_CIP_MULTI = (0x0A);
        public const Byte AB_EIP_CMD_CIP_READ = (0x4C);
        public const Byte AB_EIP_CMD_CIP_WRITE = (0x4D);
        public const Byte AB_EIP_CMD_CIP_RMW = (0x4E);
        public const Byte AB_EIP_CMD_CIP_READ_FRAG = (0x52);
        public const Byte AB_EIP_CMD_CIP_WRITE_FRAG = (0x53);
        public const Byte AB_EIP_CMD_CIP_LIST_TAGS = (0x55);

        /* flag set when command is OK */
        public const Byte AB_EIP_CMD_CIP_OK = (0x80);

        public const Byte AB_CIP_STATUS_OK = (0x00);
        public const Byte AB_CIP_STATUS_FRAG = (0x06);

        public const Byte AB_CIP_ERR_UNSUPPORTED_SERVICE = (0x08);
        public const Byte AB_CIP_ERR_PARTIAL_ERROR = (0x1e);

        /* PCCC commands */
        public const Byte AB_EIP_PCCC_TYPED_CMD = (0x0F);
        public const Byte AB_EIP_PLC5_RANGE_READ_FUNC = (0x01);
        public const Byte AB_EIP_PLC5_RANGE_WRITE_FUNC = (0x00);
        public const Byte AB_EIP_PLC5_RMW_FUNC = (0x26);
        public const Byte AB_EIP_PCCCLGX_TYPED_READ_FUNC = (0x68);
        public const Byte AB_EIP_PCCCLGX_TYPED_WRITE_FUNC = (0x67);
        public const Byte AB_EIP_SLC_RANGE_READ_FUNC = (0xA2);
        public const Byte AB_EIP_SLC_RANGE_WRITE_FUNC = (0xAA);
        public const Byte AB_EIP_SLC_RANGE_BIT_WRITE_FUNC = (0xAB);



        public const int AB_PCCC_DATA_BIT = 1;
        public const int AB_PCCC_DATA_BIT_STRING = 2;
        public const int AB_PCCC_DATA_BYTE_STRING = 3;
        public const int AB_PCCC_DATA_INT = 4;
        public const int AB_PCCC_DATA_TIMER = 5;
        public const int AB_PCCC_DATA_COUNTER = 6;
        public const int AB_PCCC_DATA_CONTROL = 7;
        public const int AB_PCCC_DATA_REAL = 8;
        public const int AB_PCCC_DATA_ARRAY = 9;
        public const int AB_PCCC_DATA_ADDRESS = 15;
        public const int AB_PCCC_DATA_BCD = 16;




        /* base data type byte values */
        /* OBSOLETE - this is now in cip.c in a table. */
        public const Byte AB_CIP_DATA_DT = (0xC0);/* DT value, 64 bit */
        public const Byte AB_CIP_DATA_BIT = (0xC1); /* Boolean value, 1 bit */
        public const Byte AB_CIP_DATA_SINT = (0xC2); /* Signed 8–bit integer value */
        public const Byte AB_CIP_DATA_INT = (0xC3); /* Signed 16–bit integer value */
        public const Byte AB_CIP_DATA_DINT = (0xC4); /* Signed 32–bit integer value */
        public const Byte AB_CIP_DATA_LINT = (0xC5); /* Signed 64–bit integer value */
        public const Byte AB_CIP_DATA_USINT = (0xC6); /* Unsigned 8–bit integer value */
        public const Byte AB_CIP_DATA_UINT = (0xC7); /* Unsigned 16–bit integer value */
        public const Byte AB_CIP_DATA_UDINT = (0xC8); /* Unsigned 32–bit integer value */
        public const Byte AB_CIP_DATA_ULINT = (0xC9); /* Unsigned 64–bit integer value */
        public const Byte AB_CIP_DATA_REAL = (0xCA); /* 32–bit floating point value, IEEE format */
        public const Byte AB_CIP_DATA_LREAL = (0xCB); /* 64–bit floating point value, IEEE format */
        public const Byte AB_CIP_DATA_STIME = (0xCC); /* Synchronous time value */
        public const Byte AB_CIP_DATA_DATE = (0xCD);/* Date value */
        public const Byte AB_CIP_DATA_TIME_OF_DAY = (0xCE); /* Time of day value */
        public const Byte AB_CIP_DATA_DATE_AND_TIME = (0xCF); /* Date and time of day value */
        public const Byte AB_CIP_DATA_STRING = (0xD0); /* Character string, 1 byte per character */
        public const Byte AB_CIP_DATA_BYTE = (0xD1); /* 8-bit bit string */
        public const Byte AB_CIP_DATA_WORD = (0xD2); /* 16-bit bit string */
        public const Byte AB_CIP_DATA_DWORD = (0xD3); /* 32-bit bit string */
        public const Byte AB_CIP_DATA_LWORD = (0xD4); /* 64-bit bit string */
        public const Byte AB_CIP_DATA_STRING2 = (0xD5); /* Wide char character string, 2 bytes per character */
        public const Byte AB_CIP_DATA_FTIME = (0xD6);/* High resolution duration value */
        public const Byte AB_CIP_DATA_LTIME = (0xD7); /* Medium resolution duration value */
        public const Byte AB_CIP_DATA_ITIME = (0xD8); /* Low resolution duration value */
        public const Byte AB_CIP_DATA_STRINGN = (0xD9); /* N-byte per char character string */
        public const Byte AB_CIP_DATA_SHORT_STRING = (0xDA); /* Counted character sting with 1 byte per character and 1 byte length indicator */
        public const Byte AB_CIP_DATA_TIME = (0xDB); /* Duration in milliseconds */
        public const Byte AB_CIP_DATA_EPATH = (0xDC);/* CIP path segment(s) */
        public const Byte AB_CIP_DATA_ENGUNIT = (0xDD); /* Engineering units */
        public const Byte AB_CIP_DATA_STRINGI = (0xDE); /* International character string (encoding?) */

        /* aggregate data type byte values */
        public const Byte AB_CIP_DATA_ABREV_STRUCT = (0xA0); /* Data is an abbreviated struct type, i.e. a CRC of the actual type descriptor */
        public const Byte AB_CIP_DATA_ABREV_ARRAY = (0xA1); /* Data is an abbreviated array type. The limits are left off */
        public const Byte AB_CIP_DATA_FULL_STRUCT = (0xA2); /* Data is a struct type descriptor */
        public const Byte AB_CIP_DATA_FULL_ARRAY = (0xA3); /* Data is an array type descriptor */


        /* transport class */
        public const Byte AB_EIP_TRANSPORT_CLASS_T3 = (0xA3);


        public const int AB_EIP_SECS_PER_TICK = 0x0A;
        public const int AB_EIP_TIMEOUT_TICKS =  0x05;
        public const int AB_EIP_VENDOR_ID = 0xF33D; /*tres 1337 */
        public const int AB_EIP_VENDOR_SN = 0x21504345;  /* the string !PCE */
        public const int AB_EIP_TIMEOUT_MULTIPLIER = 0x01;
        public const int AB_EIP_RPI = 1000000;

        //#define AB_EIP_TRANSPORT 0xA3


        /* EIP Item Types */
        public const UInt16 AB_EIP_ITEM_NAI = (0x0000); /* NULL Address Item */
        public const UInt16 AB_EIP_ITEM_CAI = (0x00A1); /* connected address item */
        public const UInt16 AB_EIP_ITEM_CDI = (0x00B1); /* connected data item */
        public const UInt16 AB_EIP_ITEM_UDI = (0x00B2); /* Unconnected data item */


    }

    abstract public class eip_generico
    {
        public byte[] _encodedData; // = new byte[46];
        static public UInt16 toWord(byte[] a, int n)
        {
            return (UInt16)(a[n] + (UInt16)(a[n + 1] * (UInt16)256));
        }

        abstract public byte[] encodedData();
        abstract public void setData(byte[] data);

    }


    public class eip_cip_co_req: eip_generico
    {


        /* encap header */  /*  Todo Litle Endian */
        public UInt16 encap_command;    /* ALWAYS 0x0070 Connected Send */
        public UInt16 encap_length;   /* packet size in bytes less the header size, which is 24 bytes */
        public UInt32 encap_session_handle;  /* from session set up */
        public UInt32 encap_status;          /* always _sent_ as 0 */
        public UInt64 encap_sender_context;  /* whatever we want to set this to, used for
                                     * identifying responses when more than one
                                     * are in flight at once.
                                     */
        public UInt32 options;               /* 0, reserved for future use */

        /* Interface Handle etc. */
        public UInt32 interface_handle;      /* ALWAYS 0 */
        public UInt16 router_timeout;        /* in seconds, zero for Connected Sends! */

        /* Common Packet Format - CPF Connected */
        public UInt16 cpf_item_count;        /* ALWAYS 2 */
        public UInt16 cpf_cai_item_type;     /* ALWAYS 0x00A1 Connected Address Item */
        public UInt16 cpf_cai_item_length;   /* ALWAYS 2 ? */
        public UInt32 cpf_targ_conn_id;           /* the connection id from Forward Open */
        public UInt16 cpf_cdi_item_type;     /* ALWAYS 0x00B1, Connected Data Item type */
        public UInt16 cpf_cdi_item_length;   /* length in bytes of the rest of the packet */

        /* Connection sequence number */
        public UInt16 cpf_conn_seq_num;      /* connection sequence ID, inc for each message */

        /* CIP Service Info */
        //uint8_t service_code;           /* ALWAYS 0x4C, CIP_READ */
        /*uint8_t req_path_size;*/          /* path size in words */
        //uint8_t req_path[ZLA_SIZE];

        public const int BASE_SIZE = 46;

        public eip_cip_co_req ()
        {
            _encodedData = new byte[BASE_SIZE];
        }
        override public byte[] encodedData()
        {
            //Console.WriteLine("Programar eip_cip_co_req.encodedData()");
            this._encodedData[0] = (byte)(encap_command & 255);
            this._encodedData[1] = (byte)((encap_command >> 8) & 255);
            this._encodedData[2] = (byte)(encap_length & 255);   /* packet size in bytes less the header size, which is 24 bytes */
            this._encodedData[3] = (byte)((encap_length >> 8) & 255);
            this._encodedData[4] = (byte)(encap_session_handle & 255);  /* from session set up */
            this._encodedData[5] = (byte)((encap_session_handle >> 8) & 255);
            this._encodedData[6] = (byte)((encap_session_handle >> 16) & 255);
            this._encodedData[7] = (byte)((encap_session_handle >> 24) & 255);
            this._encodedData[8] = (byte)(encap_status & 255);          /* always _sent_ as 0 */
            this._encodedData[9] = (byte)((encap_status >> 8) & 255);
            this._encodedData[10] = (byte)((encap_status >> 16) & 255);
            this._encodedData[11] = (byte)((encap_status >> 24) & 255);
            this._encodedData[12] = (byte)(encap_sender_context & 255);  /* whatever we want to set this to, used for
                                     * identifying responses when more than one
                                     * are in flight at once.
                                     */
            this._encodedData[13] = (byte)((encap_sender_context >> 8) & 255);
            this._encodedData[14] = (byte)((encap_sender_context >> 16) & 255);
            this._encodedData[15] = (byte)((encap_sender_context >> 24) & 255);
            this._encodedData[16] = (byte)((encap_sender_context >> 32) & 255);
            this._encodedData[17] = (byte)((encap_sender_context >> 40) & 255);
            this._encodedData[18] = (byte)((encap_sender_context >> 48) & 255);
            this._encodedData[19] = (byte)((encap_sender_context >> 56) & 255);

            this._encodedData[20] = (byte)(options & 255);               /* 0, reserved for future use */
            this._encodedData[21] = (byte)((options >> 8) & 255);
            this._encodedData[22] = (byte)((options >> 16) & 255);
            this._encodedData[23] = (byte)((options >> 24) & 255);

            /* Interface Handle etc. */
            this._encodedData[24] = (byte)(interface_handle & 255);      /* ALWAYS 0 */
            this._encodedData[25] = (byte)((interface_handle >> 8) & 255);
            this._encodedData[26] = (byte)((interface_handle >> 16) & 255);
            this._encodedData[27] = (byte)((interface_handle >> 24) & 255);

            this._encodedData[28] = (byte)(router_timeout & 255);        /* in seconds, zero for Connected Sends! */
            this._encodedData[29] = (byte)((router_timeout >> 8) & 255);

            /* Common Packet Format - CPF Connected */
            this._encodedData[30] = (byte) (cpf_item_count & 255) ;        /* ALWAYS 2 */
            this._encodedData[31] = (byte) ((cpf_item_count >>8) & 255);
            
            this._encodedData[32] = (byte) (cpf_cai_item_type & 255);     /* ALWAYS 0x00A1 Connected Address Item */
            this._encodedData[33] = (byte) ((cpf_cai_item_type >> 8) & 255);

            this._encodedData[34] = (byte) (cpf_cai_item_length & 255);   /* ALWAYS 2 ? */
            this._encodedData[35] = (byte) ((cpf_cai_item_length >> 8) & 255);

            this._encodedData[36] = (byte) (cpf_targ_conn_id & 255);           /* the connection id from Forward Open */
            this._encodedData[37] = (byte) ((cpf_targ_conn_id >> 8) & 255);
            this._encodedData[38] = (byte) ((cpf_targ_conn_id >>16) & 255);
            this._encodedData[39] = (byte) ((cpf_targ_conn_id >>24) & 255);

            this._encodedData[40] = (byte) (cpf_cdi_item_type & 255);     /* ALWAYS 0x00B1, Connected Data Item type */
            this._encodedData[41] = (byte) ((cpf_cdi_item_type >> 8) & 255);

            this._encodedData[42] = (byte) (cpf_cdi_item_length & 255);   /* length in bytes of the rest of the packet */
            this._encodedData[43] = (byte) ((cpf_cdi_item_length >> 8) & 255);

            /* Connection sequence number */
            this._encodedData[44] = (byte) (cpf_conn_seq_num & 255);      /* connection sequence ID, inc for each message */
            this._encodedData[45] = (byte) ((cpf_conn_seq_num >> 8) & 255);

            return _encodedData;
        }
        static public eip_cip_co_req createFromReq(Request req)
        {
            //Array.Copy(req.data, this.data, req.data.Length);
            return createFromData(req.data);
        }
        static public eip_cip_co_req createFromData(byte[] data)
        {
            eip_cip_co_req res = new eip_cip_co_req();

            //Array.Copy(data, res.data, data.Length);
            res.setData(data);
            return res;
        }

        override public void setData(byte[] data)
        {
            encap_command = (UInt16) (data[0] + ((UInt16)(data[1]) << 8));    /* ALWAYS 0x0070 Connected Send */
            encap_length = (UInt16)(data[2] + ((UInt16)(data[3]) << 8));    /* packet size in bytes less the header size, which is 24 bytes */
            encap_session_handle = (UInt32)(data[4] + ((UInt32)(data[5]) << 8)
                + ((UInt32)(data[6]) << 16) + ((UInt32)(data[7]) << 24));   /* from session set up */
            encap_status = (UInt32)(data[8] + ((UInt32)(data[9]) << 8)
                + ((UInt32)(data[10]) << 16) + ((UInt32)(data[11]) << 24));          /* always _sent_ as 0 */
            encap_sender_context = (UInt64)(data[12] + ((UInt64)(data[13]) << 8)
                + ((UInt64)(data[14]) << 16) + ((UInt64)(data[15]) << 24)
                + ((UInt64)(data[16]) << 32) + ((UInt64)(data[17]) << 40)
                + ((UInt64)(data[18]) << 48) + ((UInt64)(data[19]) << 56));  /* whatever we want to set this to, used for
                                     * identifying responses when more than one
                                     * are in flight at once.
                                     */
            options = (UInt32)(data[20] + ((UInt32)(data[21]) << 8)
                + ((UInt32)(data[22]) << 16) + ((UInt32)(data[23]) << 24));                /* 0, reserved for future use */

            /* Interface Handle etc. */
            interface_handle = (UInt32)(data[24] + ((UInt32)(data[25]) << 8)
                + ((UInt32)(data[26]) << 16) + ((UInt32)(data[27]) << 24)); ;      /* ALWAYS 0 */
            router_timeout = (UInt16)(data[28] + ((UInt16)(data[29]) << 8));        /* in seconds, zero for Connected Sends! */

            /* Common Packet Format - CPF Connected */
            cpf_item_count = (UInt16)(data[30] + ((UInt16)(data[31]) << 8));        /* ALWAYS 2 */
            cpf_cai_item_type = (UInt16)(data[32] + ((UInt16)(data[33]) << 8));     /* ALWAYS 0x00A1 Connected Address Item */
            cpf_cai_item_length = (UInt16)(data[34] + ((UInt16)(data[35]) << 8));   /* ALWAYS 2 ? */
            cpf_targ_conn_id = (UInt32)(data[36] + ((UInt32)(data[37]) << 8)
                + ((UInt32)(data[38]) << 16) + ((UInt32)(data[39]) << 24));           /* the connection id from Forward Open */
            cpf_cdi_item_type = (UInt16)(data[40] + ((UInt16)(data[41]) << 8));     /* ALWAYS 0x00B1, Connected Data Item type */
            cpf_cdi_item_length = (UInt16)(data[42] + ((UInt16)(data[43]) << 8));   /* length in bytes of the rest of the packet */

            /* Connection sequence number */
            cpf_conn_seq_num = (UInt16)(data[44] + ((UInt16)(data[45]) << 8));      /* connection sequence ID, inc for each message */

            encodedData();
    }

    /*static public eip_cip_co_req createFromReq(Request req)
    {
        eip_cip_co_req e = new eip_cip_co_req();

        e.encap_command = toWord(req.data, 0);
        e.encap_length = toWord(req.data, 2);

        Console.WriteLine("----->>>>  Terminar de implementar eip_cip_co_req");


        return new eip_cip_co_req();
    }*/
}

    /* Session Registration Request */
    public class eip_session_reg_req
    {
        //  Todo Little Endian
        /* encap header */
        public UInt16 encap_command;         /* ALWAYS 0x0065 Register Session*/
        public UInt16 encap_length;   /* packet size in bytes - 24 */
        public UInt32 encap_session_handle;  /* from session set up */
        public UInt32 encap_status;          /* always _sent_ as 0 */
        public UInt64 encap_sender_context;  /* whatever we want to set this to, used for
                                     * identifying responses when more than one
                                     * are in flight at once.
                                     */
        public UInt32 encap_options;         /* 0, reserved for future use */

        /* session registration request */
        public UInt16 eip_version;
        public UInt16 option_flags;

        public byte[] data = new byte[28];

        public const int size = 28;

        public byte[] getData()
        {
            data[0] = (byte) (encap_command & 255);
            data[1] = (byte) (encap_command >> 8 & 255);
            data[2] = (byte) (encap_length & 255);
            data[3] = (byte) (encap_length >>8 & 255);
            data[4] = (byte) (encap_session_handle & 255);
            data[5] = (byte)(encap_session_handle >> 8 & 255);
            data[6] = (byte)(encap_session_handle >> 16 & 255);
            data[7] = (byte)(encap_session_handle >> 24 & 255);
            data[8] = (byte)(encap_status & 255);
            data[9] = (byte)(encap_status >> 8 & 255);
            data[10] = (byte)(encap_status >> 16 & 255);
            data[11] = (byte)(encap_status >> 24 & 255);
            data[12] = (byte)(encap_sender_context & 255);
            data[13] = (byte)(encap_sender_context >> 8 & 255);
            data[14] = (byte)(encap_sender_context >> 16 & 255);
            data[15] = (byte)(encap_sender_context >> 24 & 255);
            data[16] = (byte)(encap_sender_context >> 32 & 255);
            data[17] = (byte)(encap_sender_context >> 40 & 255);
            data[18] = (byte)(encap_sender_context >> 48 & 255);
            data[19] = (byte)(encap_sender_context >> 56 & 255);
            data[20] = (byte)(encap_options & 255);
            data[21] = (byte)(encap_options >> 8 & 255);
            data[22] = (byte)(encap_options >> 16 & 255);
            data[23] = (byte)(encap_options >> 24 & 255);
            data[24] = (byte)(eip_version & 255);
            data[25] = (byte)(eip_version >> 8 & 255);
            data[26] = (byte)(option_flags & 255);
            data[27] = (byte)(option_flags >> 8 & 255);

            return data;
        }

        
        }

    }

/* EIP Encapsulation Header */
public class eip_encap: eip_generico
{
    //  Todo es LE.
    public UInt16 encap_command;
    public UInt16 encap_length;
    public UInt32 encap_session_handle;
    public UInt32 encap_status;
    public UInt64 encap_sender_context;
    public UInt32 encap_options;

    public byte[] data = new byte[24];

    public int size = 24;
    public const /*static*/ int encap_size = 24;
    virtual public byte[] getData()
    {
        data[0] = (byte)(encap_command & 255);
        data[1] = (byte)(encap_command >> 8 & 255);
        data[2] = (byte)(encap_length & 255);
        data[3] = (byte)(encap_length >> 8 & 255);
        data[4] = (byte)(encap_session_handle & 255);
        data[5] = (byte)(encap_session_handle >> 8 & 255);
        data[6] = (byte)(encap_session_handle >> 16 & 255);
        data[7] = (byte)(encap_session_handle >> 24 & 255);
        data[8] = (byte)(encap_status & 255);
        data[9] = (byte)(encap_status >> 8 & 255);
        data[10] = (byte)(encap_status >> 16 & 255);
        data[11] = (byte)(encap_status >> 24 & 255);
        data[12] = (byte)(encap_sender_context & 255);
        data[13] = (byte)(encap_sender_context >> 8 & 255);
        data[14] = (byte)(encap_sender_context >> 16 & 255);
        data[15] = (byte)(encap_sender_context >> 24 & 255);
        data[16] = (byte)(encap_sender_context >> 32 & 255);
        data[17] = (byte)(encap_sender_context >> 40 & 255);
        data[18] = (byte)(encap_sender_context >> 48 & 255);
        data[19] = (byte)(encap_sender_context >> 56 & 255);
        data[20] = (byte)(encap_options & 255);
        data[21] = (byte)(encap_options >> 8 & 255);
        data[22] = (byte)(encap_options >> 16 & 255);
        data[23] = (byte)(encap_options >> 24 & 255);

        return data;
    }

    /*internal static eip_encap setData(byte[] data)
    {
        eip_encap e = new eip_encap();
        Array.Copy(data, 0, e.data, 0, size);
        e.encap_command = (UInt16)(data[0] + ((UInt16)data[1] << 8));
        e.encap_length = (UInt16)(data[2] + ((UInt16)data[3] << 8));
        e.encap_session_handle = (UInt32)(data[4] + ((UInt32)data[5] << 8) + ((UInt32)data[6] << 16) +
            ((UInt32)data[7] << 24));
        e.encap_status = (UInt32)(data[8] + ((UInt32)data[9] << 8) + ((UInt32)data[10] << 16) +
            ((UInt32)data[11] << 24));
        e.encap_sender_context = (UInt64)(data[12] + ((UInt64)data[13] << 8) + ((UInt64)data[14] << 16) +
            ((UInt64)data[15] << 24) + ((UInt64)data[16] << 32) + ((UInt64)data[17] << 40) + ((UInt64)data[18] << 48) +
            ((UInt64)data[19] << 56));
        e.encap_options = (UInt32)(data[20] + ((UInt32)data[21] << 8) + ((UInt32)data[22] << 16) +
            ((UInt32)data[23] << 24));

        e.getData();

        return e;
    }*/

    public override byte[] encodedData()
    {
        throw new NotImplementedException();
    }

    public override void setData(byte[] data)
    {
//        eip_encap e = new eip_encap();
        Array.Copy(data, 0, this.data, 0, encap_size);
        this.encap_command = (UInt16)(data[0] + ((UInt16)data[1] << 8));
        this.encap_length = (UInt16)(data[2] + ((UInt16)data[3] << 8));
        encap_session_handle = (UInt32)(data[4] + ((UInt32)data[5] << 8) + ((UInt32)data[6] << 16) +
            ((UInt32)data[7] << 24));
        encap_status = (UInt32)(data[8] + ((UInt32)data[9] << 8) + ((UInt32)data[10] << 16) +
            ((UInt32)data[11] << 24));
        encap_sender_context = (UInt64)(data[12] + ((UInt64)data[13] << 8) + ((UInt64)data[14] << 16) +
            ((UInt64)data[15] << 24) + ((UInt64)data[16] << 32) + ((UInt64)data[17] << 40) + ((UInt64)data[18] << 48) +
            ((UInt64)data[19] << 56));
        encap_options = (UInt32)(data[20] + ((UInt32)data[21] << 8) + ((UInt32)data[22] << 16) +
            ((UInt32)data[23] << 24));

        getData();

//        return e;

//        throw new NotImplementedException();
    }
    static public eip_encap createFromReq(Request req)
    {
        //Array.Copy(req.data, this.data, req.data.Length);
        return createFromData(req.data);
    }
    static public eip_encap createFromData(byte[] data)
    {
        eip_encap res = new eip_encap();

        //Array.Copy(data, res.data, data.Length);
        res.setData(data);
        return res;
    }

}
public class cip_multi_req_header : eip_generico {
    public byte service_code;        /* ALWAYS 0x0A Forward Open Request */
    public byte req_path_size;       /* ALWAYS 2, size in words of path, next field */
    public byte[] req_path = new byte[4];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/
    public UInt16 request_count;        /* number of requests packed in this packet. */
    public UInt16[] request_offsets;    /* request offsets from the count */
    public byte[] data;
    public /*static*/ const byte size = 8;

    public override byte[] encodedData()
    {
        throw new NotImplementedException();
    }

    public override void setData(byte[] data)
    {
        throw new NotImplementedException();
    }

}

public class eip_forward_open_request_t:eip_encap
{

    /* Forward Open Request */
    /* encap header */
    //uint16_le encap_command;    /* ALWAYS 0x006f Unconnected Send*/
    //uint16_le encap_length;   /* packet size in bytes - 24 */
    //uint32_le encap_session_handle;  /* from session set up */
    //uint32_le encap_status;          /* always _sent_ as 0 */
    //uint64_le encap_sender_context;  /* whatever we want to set this to, used for
    //                                 * identifying responses when more than one
    //                                 * are in flight at once.
    //                                 */
    //uint32_le encap_options;         /* 0, reserved for future use */

    public UInt32 interface_handle_offs = 24;

    /* Interface Handle etc. */
    public UInt32 interface_handle;      /* ALWAYS 0 */
    public UInt16 router_timeout;        /* in seconds */

    /* Common Packet Format - CPF Unconnected */
    public UInt16 cpf_item_count;        /* ALWAYS 2 */
    public UInt16 cpf_nai_item_type;     /* ALWAYS 0 */
    public UInt16 cpf_nai_item_length;   /* ALWAYS 0 */
    public UInt16 cpf_udi_item_type;     /* ALWAYS 0x00B2 - Unconnected Data Item */
    public UInt16 cpf_udi_item_length;   /* REQ: fill in with length of remaining data. */

    /* CM Service Request - Connection Manager */
    public UInt32 cm_service_code_offs = 40;
    public byte cm_service_code;        /* ALWAYS 0x54 Forward Open Request */
    public byte cm_req_path_size;       /* ALWAYS 2, size in words of path, next field */
    public byte[] cm_req_path = new byte[4];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/

    /* Forward Open Params */
    public byte secs_per_tick;          /* seconds per tick */
    public byte timeout_ticks;          /* timeout = srd_secs_per_tick * src_timeout_ticks */
    public UInt32 orig_to_targ_conn_id;  /* 0, returned by target in reply. */
    public UInt32 targ_to_orig_conn_id;  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
    public UInt16 conn_serial_number;    /* our connection ID/serial number */
        public UInt16 orig_vendor_id;        /* our unique vendor ID */
    public UInt32 orig_serial_number;    /* our unique serial number */
    public byte conn_timeout_multiplier;/* timeout = mult * RPI */
    public byte[] reserved = new byte[3];            /* reserved, set to 0 */
    public UInt32 orig_to_targ_rpi;      /* us to target RPI - Request Packet Interval in microseconds */
    public UInt16 orig_to_targ_conn_params; /* some sort of identifier of what kind of PLC we are??? */
    public UInt32 targ_to_orig_rpi;      /* target to us RPI, in microseconds */
    public UInt16 targ_to_orig_conn_params; /* some sort of identifier of what kind of PLC the target is ??? */
    public byte transport_class;        /* ALWAYS 0xA3, server transport, class 3, application trigger */
    public byte path_size;              /* size of connection path in 16-bit words
                                     * connection path from MSG instruction.
                                     *
                                     * EG LGX with 1756-ENBT and CPU in slot 0 would be:
                                     * 0x01 - backplane port of 1756-ENBT
                                     * 0x00 - slot 0 for CPU
                                     * 0x20 - class
                                     * 0x02 - MR Message Router
                                     * 0x24 - instance
                                     * 0x01 - instance #1.
                                     */

    //uint8_t conn_path[ZLA_SIZE];    /* connection path as above */
    public eip_forward_open_request_t() : base() 
    {
        /*public const byte*/
        size += 58;
        data = new byte[size];
        for (int i = 0; i < size; i++)
        {
            data[i] = 0;
        }
        //Array.Fill<byte>(data, 0);
    }

    override public byte[] getData()
    {
        base.getData();

        /*
        data[0] = (byte)(encap_command & 255);
        data[1] = (byte)(encap_command >> 8 & 255);
        data[2] = (byte)(encap_length & 255);
        data[3] = (byte)(encap_length >> 8 & 255);
        data[4] = (byte)(encap_session_handle & 255);
        data[5] = (byte)(encap_session_handle >> 8 & 255);
        data[6] = (byte)(encap_session_handle >> 16 & 255);
        data[7] = (byte)(encap_session_handle >> 24 & 255);
        data[8] = (byte)(encap_status & 255);
        data[9] = (byte)(encap_status >> 8 & 255);
        data[10] = (byte)(encap_status >> 16 & 255);
        data[11] = (byte)(encap_status >> 24 & 255);
        data[12] = (byte)(encap_sender_context & 255);
        data[13] = (byte)(encap_sender_context >> 8 & 255);
        data[14] = (byte)(encap_sender_context >> 16 & 255);
        data[15] = (byte)(encap_sender_context >> 24 & 255);
        data[16] = (byte)(encap_sender_context >> 32 & 255);
        data[17] = (byte)(encap_sender_context >> 40 & 255);
        data[18] = (byte)(encap_sender_context >> 48 & 255);
        data[19] = (byte)(encap_sender_context >> 56 & 255);
        data[20] = (byte)(encap_options & 255);
        data[21] = (byte)(encap_options >> 8 & 255);
        data[22] = (byte)(encap_options >> 16 & 255);
        data[23] = (byte)(encap_options >> 24 & 255);
        */




        data[24] = (byte)(interface_handle & 255);
        data[25] = (byte)(interface_handle >> 8 & 255);
        data[26] = (byte)(interface_handle >> 16 & 255);
        data[27] = (byte)(interface_handle >> 24 & 255);
        data[28] = (byte)(router_timeout & 255);        /* in seconds */
        data[29] = (byte)(router_timeout >> 8 & 255);        /* in seconds */
        data[30] = (byte)(cpf_item_count & 255);        /* ALWAYS 2 */
        data[31] = (byte)(cpf_item_count >> 8 & 255);        /* ALWAYS 2 */
        data[32] = (byte)(cpf_nai_item_type & 255);     /* ALWAYS 0 */
        data[33] = (byte)(cpf_nai_item_type >> 8 & 255);     /* ALWAYS 0 */
        data[34] = (byte)(cpf_nai_item_length & 255);   /* ALWAYS 0 */
        data[35] = (byte)(cpf_nai_item_length >> 8 & 255);   /* ALWAYS 0 */
        data[36] = (byte)(cpf_udi_item_type & 255);     /* ALWAYS 0x00B2 - Unconnected Data Item */
        data[37] = (byte)(cpf_udi_item_type >> 8 & 255);     /* ALWAYS 0x00B2 - Unconnected Data Item */
        data[38] = (byte)(cpf_udi_item_length & 255);   /* REQ: fill in with length of remaining data. */
        data[39] = (byte)(cpf_udi_item_length >> 8 & 255);   /* REQ: fill in with length of remaining data. */

        /* CM Service Request - Connection Manager */
        data[40] = cm_service_code;        /* ALWAYS 0x54 Forward Open Request */
        data[41] = cm_req_path_size;       /* ALWAYS 2, size in words of path, next field */
        data[42] = cm_req_path[0];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/
        data[43] = cm_req_path[1];        /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/
        data[44] = cm_req_path[2];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/
        data[45] = cm_req_path[3];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/

        /* Forward Open Params */
        data[46] = secs_per_tick;          /* seconds per tick */
        data[47] = timeout_ticks;          /* timeout = srd_secs_per_tick * src_timeout_ticks */
        data[48] = (byte)(orig_to_targ_conn_id & 255);  /* 0, returned by target in reply. */
        data[49] = (byte)(orig_to_targ_conn_id >> 8 & 255);  /* 0, returned by target in reply. */
        data[50] = (byte)(orig_to_targ_conn_id >> 16 & 255);  /* 0, returned by target in reply. */
        data[51] = (byte)(orig_to_targ_conn_id >> 24 & 255);  /* 0, returned by target in reply. */
        data[52] = (byte)(targ_to_orig_conn_id & 255);  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
        data[53] = (byte)(targ_to_orig_conn_id >> 8 & 255);  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
        data[54] = (byte)(targ_to_orig_conn_id >> 16 & 255);  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
        data[55] = (byte)(targ_to_orig_conn_id >> 24 & 255);  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
        data[56] = (byte)(conn_serial_number & 255);    /* our connection ID/serial number ?? */
        data[57] = (byte)(conn_serial_number >> 8 & 255);    /* our connection ID/serial number ?? */
        data[58] = (byte)(orig_vendor_id & 255);        /* our unique vendor ID */
        data[59] = (byte)(orig_vendor_id >> 8 & 255);        /* our unique vendor ID */
        data[60] = (byte)(orig_serial_number & 255);    /* our unique serial number */
        data[61] = (byte)(orig_serial_number >> 8 & 255);    /* our unique serial number */
        data[62] = (byte)(orig_serial_number >> 16 & 255);    /* our unique serial number */
        data[63] = (byte)(orig_serial_number >> 24 & 255);    /* our unique serial number */  

        
        data[64] = conn_timeout_multiplier;/* timeout = mult * RPI */
        data[65] = reserved[0];            /* reserved, set to 0 */
        data[66] = reserved[1];            /* reserved, set to 0 */
        data[67] = reserved[2];            /* reserved, set to 0 */

       

        data[68] = (byte)(orig_to_targ_rpi & 255);      /* us to target RPI - Request Packet Interval in microseconds */
        data[69] = (byte)(orig_to_targ_rpi >> 8 & 255);      /* us to target RPI - Request Packet Interval in microseconds */
        data[70] = (byte)(orig_to_targ_rpi >> 16 & 255);      /* us to target RPI - Request Packet Interval in microseconds */
        data[71] = (byte)(orig_to_targ_rpi >> 24 & 255);      /* us to target RPI - Request Packet Interval in microseconds */
        data[72] = (byte)(orig_to_targ_conn_params & 255); /* some sort of identifier of what kind of PLC we are??? */
        data[73] = (byte)(orig_to_targ_conn_params >> 8 & 255); /* some sort of identifier of what kind of PLC we are??? */
        data[74] = (byte)(targ_to_orig_rpi & 255);      /* target to us RPI, in microseconds */
        data[75] = (byte)(targ_to_orig_rpi >> 8 & 255);      /* target to us RPI, in microseconds */
        data[76] = (byte)(targ_to_orig_rpi >> 16 & 255);      /* target to us RPI, in microseconds */
        data[77] = (byte)(targ_to_orig_rpi >> 24 & 255);      /* target to us RPI, in microseconds */
        data[78] = (byte)(targ_to_orig_conn_params & 255); /* some sort of identifier of what kind of PLC the target is ??? */
        data[79] = (byte)(targ_to_orig_conn_params >> 8 & 255); /* some sort of identifier of what kind of PLC the target is ??? */
        data[80] = transport_class;        /* ALWAYS 0xA3, server transport, class 3, application trigger */
        data[81] = path_size;              /* size of connection path in 16-bit words
                                     * connection path from MSG instruction.
                                     *
                                     * EG LGX with 1756-ENBT and CPU in slot 0 would be:
                                     * 0x01 - backplane port of 1756-ENBT
                                     * 0x00 - slot 0 for CPU
                                     * 0x20 - class
                                     * 0x02 - MR Message Router
                                     * 0x24 - instance
                                     * 0x01 - instance #1.
                                     */

        return data;
    }

    public override byte[] encodedData()
    {
        throw new NotImplementedException();
    }

    public override void setData(byte[] data)
    {
        //throw new NotImplementedException();
        //        eip_encap e = new eip_encap();
        //Array.Copy(data, 0, this.data, 0, size);
        base.setData(data);

        /*this.encap_command = (UInt16)(data[0] + ((UInt16)data[1] << 8));
        this.encap_length = (UInt16)(data[2] + ((UInt16)data[3] << 8));
        encap_session_handle = (UInt32)(data[4] + ((UInt32)data[5] << 8) + ((UInt32)data[6] << 16) +
            ((UInt32)data[7] << 24));
        encap_status = (UInt32)(data[8] + ((UInt32)data[9] << 8) + ((UInt32)data[10] << 16) +
            ((UInt32)data[11] << 24));
        encap_sender_context = (UInt64)(data[12] + ((UInt64)data[13] << 8) + ((UInt64)data[14] << 16) +
            ((UInt64)data[15] << 24) + ((UInt64)data[16] << 32) + ((UInt64)data[17] << 40) + ((UInt64)data[18] << 48) +
            ((UInt64)data[19] << 56));
        encap_options = (UInt32)(data[20] + ((UInt32)data[21] << 8) + ((UInt32)data[22] << 16) +
            ((UInt32)data[23] << 24));*/


        /* Interface Handle etc. */

        interface_handle = (UInt32)(data[24] + ((UInt32)data[25] << 8) + ((UInt32)data[26] << 16) +
            ((UInt32)data[27] << 24));       /* ALWAYS 0 */
        router_timeout = (UInt16)(data[28] + ((UInt16)data[29] << 8));        /* in seconds */

        /* Common Packet Format - CPF Unconnected */
        cpf_item_count = (UInt16)(data[30] + ((UInt16)data[31] << 8));        /* ALWAYS 2 */
        cpf_nai_item_type = (UInt16)(data[32] + ((UInt16)data[33] << 8));     /* ALWAYS 0 */
        cpf_nai_item_length = (UInt16)(data[34] + ((UInt16)data[35] << 8));   /* ALWAYS 0 */
        cpf_udi_item_type = (UInt16)(data[36] + ((UInt16)data[37] << 8));     /* ALWAYS 0x00B2 - Unconnected Data Item */
        cpf_udi_item_length = (UInt16)(data[38] + ((UInt16)data[39] << 8));   /* REQ: fill in with length of remaining data. */

        /* CM Service Request - Connection Manager */
        cm_service_code = data[40];        /* ALWAYS 0x54 Forward Open Request */
        cm_req_path_size = data[41];       /* ALWAYS 2, size in words of path, next field */
        cm_req_path[0] = data[42];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/
        cm_req_path[1] = data[43];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/
        cm_req_path[2] = data[44];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/
        cm_req_path[3] = data[45];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/

        /* Forward Open Params */
        secs_per_tick = data[46];          /* seconds per tick */
        timeout_ticks = data[47];          /* timeout = srd_secs_per_tick * src_timeout_ticks */
        orig_to_targ_conn_id = (UInt32)(data[48] + ((UInt32)data[49] << 8) + ((UInt32)data[50] << 16) +
            ((UInt32)data[51] << 24));  /* 0, returned by target in reply. */
        targ_to_orig_conn_id = (UInt32)(data[52] + ((UInt32)data[53] << 8) + ((UInt32)data[54] << 16) +
            ((UInt32)data[55] << 24));  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
        conn_serial_number = (UInt16)(data[56] + ((UInt16)data[57] << 8));    /* our connection ID/serial number ?? */
        orig_vendor_id = (UInt16)(data[58] + ((UInt16)data[59] << 8));        /* our unique vendor ID */
        orig_serial_number = (UInt32)(data[60] + ((UInt32)data[61] << 8) + ((UInt32)data[62] << 16) +
            ((UInt32)data[63] << 24));    /* our unique serial number */
        conn_timeout_multiplier = data[64];/* timeout = mult * RPI */
        reserved[0] = data[65];            /* reserved, set to 0 */
        reserved[1] = data[66];            /* reserved, set to 0 */
        reserved[2] = data[67];            /* reserved, set to 0 */
        orig_to_targ_rpi = (UInt32)(data[68] + ((UInt32)data[69] << 8) + ((UInt32)data[70] << 16) +
            ((UInt32)data[71] << 24));      /* us to target RPI - Request Packet Interval in microseconds */
        orig_to_targ_conn_params = (UInt16)(data[72] + ((UInt16)data[73] << 8)); /* some sort of identifier of what kind of PLC we are??? */
        targ_to_orig_rpi = (UInt32)(data[74] + ((UInt32)data[75] << 8) + ((UInt32)data[76] << 16) +
            ((UInt32)data[77] << 24));      /* target to us RPI, in microseconds */
        targ_to_orig_conn_params = (UInt16)(data[78] + ((UInt16)data[79] << 8)); /* some sort of identifier of what kind of PLC the target is ??? */
        transport_class = data[80];        /* ALWAYS 0xA3, server transport, class 3, application trigger */
        path_size = data[81];              /* size of connection path in 16-bit words
                                     * connection path from MSG instruction.
                                     *
                                     * EG LGX with 1756-ENBT and CPU in slot 0 would be:
                                     * 0x01 - backplane port of 1756-ENBT
                                     * 0x00 - slot 0 for CPU
                                     * 0x20 - class
                                     * 0x02 - MR Message Router
                                     * 0x24 - instance
                                     * 0x01 - instance #1.
                                     */

        getData();

        //        return e;

        //        throw new NotImplementedException();
    }
    static public eip_encap createFromReq(Request req)
    {
        throw new NotImplementedException();

        //Array.Copy(req.data, this.data, req.data.Length);
        return createFromData(req.data);
    }
    static public eip_encap createFromData(byte[] data)
    {
        throw new NotImplementedException();

        eip_encap res = new eip_encap();

        //Array.Copy(data, res.data, data.Length);
        res.setData(data);
        return res;
    }

}


public class eip_forward_open_request_ex_t : eip_encap
{
    /* Forward Open Request Extended */

    /* encap header */
    /*uint16_le encap_command;    /* ALWAYS 0x006f Unconnected Send*/
    /*uint16_le encap_length;   /* packet size in bytes - 24 */
    /*uint32_le encap_session_handle;  /* from session set up */
    /*uint32_le encap_status;          /* always _sent_ as 0 */
    /*uint64_le encap_sender_context;  /* whatever we want to set this to, used for
                                 * identifying responses when more than one
                                 * are in flight at once.
                                 */
    /*uint32_le encap_options;         /* 0, reserved for future use */

    /* Interface Handle etc. */
    public UInt32 interface_handle_offs = 24;

    public UInt32 interface_handle;      /* ALWAYS 0 */
    public UInt16 router_timeout;        /* in seconds */

    /* Common Packet Format - CPF Unconnected */
    public UInt16 cpf_item_count;        /* ALWAYS 2 */
    public UInt16 cpf_nai_item_type;     /* ALWAYS 0 */
    public UInt16 cpf_nai_item_length;   /* ALWAYS 0 */
    public UInt16 cpf_udi_item_type;     /* ALWAYS 0x00B2 - Unconnected Data Item */
    public UInt16 cpf_udi_item_length;   /* REQ: fill in with length of remaining data. */

    /* CM Service Request - Connection Manager */
    public UInt32 cm_service_code_offs = 40;
    public byte cm_service_code;        /* ALWAYS 0x54 Forward Open Request */
    public byte cm_req_path_size;       /* ALWAYS 2, size in words of path, next field */
    public byte[] cm_req_path = new byte[4];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/

    /* Forward Open Params */
    public byte secs_per_tick;          /* seconds per tick */
    public byte timeout_ticks;          /* timeout = srd_secs_per_tick * src_timeout_ticks */
    public UInt32 orig_to_targ_conn_id;  /* 0, returned by target in reply. */
    public UInt32 targ_to_orig_conn_id;  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
    public UInt16 conn_serial_number;    /* our connection ID/serial number ?? */
    public UInt16 orig_vendor_id;        /* our unique vendor ID */
    public UInt32 orig_serial_number;    /* our unique serial number */
    public byte conn_timeout_multiplier;/* timeout = mult * RPI */
    public byte[] reserved = new byte[3];            /* reserved, set to 0 */
    public UInt32 orig_to_targ_rpi;      /* us to target RPI - Request Packet Interval in microseconds */
    public UInt32 orig_to_targ_conn_params_ex; /* some sort of identifier of what kind of PLC we are??? */
    public UInt32 targ_to_orig_rpi;      /* target to us RPI, in microseconds */
    public UInt32 targ_to_orig_conn_params_ex; /* some sort of identifier of what kind of PLC the target is ??? */
    public byte transport_class;        /* ALWAYS 0xA3, server transport, class 3, application trigger */
    public byte path_size;              /* size of connection path in 16-bit words
                                     * connection path from MSG instruction.
                                     *
                                     * EG LGX with 1756-ENBT and CPU in slot 0 would be:
                                     * 0x01 - backplane port of 1756-ENBT
                                     * 0x00 - slot 0 for CPU
                                     * 0x20 - class
                                     * 0x02 - MR Message Router
                                     * 0x24 - instance
                                     * 0x01 - instance #1.
                                     */

    public eip_forward_open_request_ex_t() : base() //uint8_t conn_path[ZLA_SIZE];    /* connection path as above */
    {
        /*public const byte*/
        size += 62;
        data = new byte[size];
        for (int i = 0; i < size; i++)
        {
            data[i] = 0;
        }
        //Array.Fill<byte>(data, 0);
    }

    override public byte[] getData()
    {
        base.getData();


        /*
        data[0] = (byte)(encap_command & 255);
        data[1] = (byte)(encap_command >> 8 & 255);
        data[2] = (byte)(encap_length & 255);
        data[3] = (byte)(encap_length >> 8 & 255);
        data[4] = (byte)(encap_session_handle & 255);
        data[5] = (byte)(encap_session_handle >> 8 & 255);
        data[6] = (byte)(encap_session_handle >> 16 & 255);
        data[7] = (byte)(encap_session_handle >> 24 & 255);
        data[8] = (byte)(encap_status & 255);
        data[9] = (byte)(encap_status >> 8 & 255);
        data[10] = (byte)(encap_status >> 16 & 255);
        data[11] = (byte)(encap_status >> 24 & 255);
        data[12] = (byte)(encap_sender_context & 255);
        data[13] = (byte)(encap_sender_context >> 8 & 255);
        data[14] = (byte)(encap_sender_context >> 16 & 255);
        data[15] = (byte)(encap_sender_context >> 24 & 255);
        data[16] = (byte)(encap_sender_context >> 32 & 255);
        data[17] = (byte)(encap_sender_context >> 40 & 255);
        data[18] = (byte)(encap_sender_context >> 48 & 255);
        data[19] = (byte)(encap_sender_context >> 56 & 255);
        data[20] = (byte)(encap_options & 255);
        data[21] = (byte)(encap_options >> 8 & 255);
        data[22] = (byte)(encap_options >> 16 & 255);
        data[23] = (byte)(encap_options >> 24 & 255);
        */

        data[24] = (byte)(interface_handle & 255);
        data[25] = (byte)(interface_handle >> 8 & 255);
        data[26] = (byte)(interface_handle >> 16 & 255);
        data[27] = (byte)(interface_handle >> 24 & 255);
        data[28] = (byte)(router_timeout & 255);        /* in seconds */
        data[29] = (byte)(router_timeout >> 8 & 255);        /* in seconds */
        data[30] = (byte)(cpf_item_count & 255);        /* ALWAYS 2 */
        data[31] = (byte)(cpf_item_count >> 8 & 255);        /* ALWAYS 2 */
        data[32] = (byte)(cpf_nai_item_type & 255);     /* ALWAYS 0 */
        data[33] = (byte)(cpf_nai_item_type >> 8 & 255);     /* ALWAYS 0 */
        data[34] = (byte)(cpf_nai_item_length & 255);   /* ALWAYS 0 */
        data[35] = (byte)(cpf_nai_item_length >> 8 & 255);   /* ALWAYS 0 */
        data[36] = (byte)(cpf_udi_item_type & 255);     /* ALWAYS 0x00B2 - Unconnected Data Item */
        data[37] = (byte)(cpf_udi_item_type >> 8 & 255);     /* ALWAYS 0x00B2 - Unconnected Data Item */
        data[38] = (byte)(cpf_udi_item_length & 255);   /* REQ: fill in with length of remaining data. */
        data[39] = (byte)(cpf_udi_item_length >> 8 & 255);   /* REQ: fill in with length of remaining data. */

        /* CM Service Request - Connection Manager */
        data[40] = cm_service_code;        /* ALWAYS 0x54 Forward Open Request */
        data[41] = cm_req_path_size;       /* ALWAYS 2, size in words of path, next field */
        data[42] = cm_req_path[0];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/
        data[43] = cm_req_path[1];        /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/
        data[44] = cm_req_path[2];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/
        data[45] = cm_req_path[3];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/

        /* Forward Open Params */
        data[46] = secs_per_tick;          /* seconds per tick */
        data[47] = timeout_ticks;          /* timeout = srd_secs_per_tick * src_timeout_ticks */
        data[48] = (byte)(orig_to_targ_conn_id & 255);  /* 0, returned by target in reply. */
        data[49] = (byte)(orig_to_targ_conn_id >> 8 & 255);  /* 0, returned by target in reply. */
        data[50] = (byte)(orig_to_targ_conn_id >> 16 & 255);  /* 0, returned by target in reply. */
        data[51] = (byte)(orig_to_targ_conn_id >> 24 & 255);  /* 0, returned by target in reply. */
        data[52] = (byte)(targ_to_orig_conn_id & 255);  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
        data[53] = (byte)(targ_to_orig_conn_id >> 8 & 255);  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
        data[54] = (byte)(targ_to_orig_conn_id >> 16 & 255);  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
        data[55] = (byte)(targ_to_orig_conn_id >> 24 & 255);  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
        data[56] = (byte)(conn_serial_number & 255);    /* our connection ID/serial number ?? */
        data[57] = (byte)(conn_serial_number >> 8 & 255);    /* our connection ID/serial number ?? */
        data[58] = (byte)(orig_vendor_id & 255);        /* our unique vendor ID */
        data[59] = (byte)(orig_vendor_id >> 8 & 255);        /* our unique vendor ID */
        data[60] = (byte)(orig_serial_number & 255);    /* our unique serial number */
        data[61] = (byte)(orig_serial_number >> 8 & 255);    /* our unique serial number */
        data[62] = (byte)(orig_serial_number >> 16 & 255);    /* our unique serial number */
        data[63] = (byte)(orig_serial_number >> 24 & 255);    /* our unique serial number */
        data[64] = conn_timeout_multiplier;/* timeout = mult * RPI */
        data[65] = reserved[0];            /* reserved, set to 0 */
        data[66] = reserved[1];            /* reserved, set to 0 */
        data[67] = reserved[2];            /* reserved, set to 0 */
        data[68] = (byte)(orig_to_targ_rpi & 255);      /* us to target RPI - Request Packet Interval in microseconds */
        data[69] = (byte)(orig_to_targ_rpi >> 8 & 255);      /* us to target RPI - Request Packet Interval in microseconds */
        data[70] = (byte)(orig_to_targ_rpi >> 16 & 255);      /* us to target RPI - Request Packet Interval in microseconds */
        data[71] = (byte)(orig_to_targ_rpi >> 24 & 255);      /* us to target RPI - Request Packet Interval in microseconds */
        data[72] = (byte)(orig_to_targ_conn_params_ex & 255); /* some sort of identifier of what kind of PLC we are??? */
        data[73] = (byte)(orig_to_targ_conn_params_ex >> 8 & 255); /* some sort of identifier of what kind of PLC we are??? */
        data[74] = (byte)(orig_to_targ_conn_params_ex >> 16 & 255); /* some sort of identifier of what kind of PLC we are??? */
        data[75] = (byte)(orig_to_targ_conn_params_ex >> 24 & 255); /* some sort of identifier of what kind of PLC we are??? */
        data[76] = (byte)(targ_to_orig_rpi & 255);      /* target to us RPI, in microseconds */
        data[77] = (byte)(targ_to_orig_rpi >> 8 & 255);      /* target to us RPI, in microseconds */
        data[78] = (byte)(targ_to_orig_rpi >> 16 & 255);      /* target to us RPI, in microseconds */
        data[79] = (byte)(targ_to_orig_rpi >> 24 & 255);      /* target to us RPI, in microseconds */
        data[80] = (byte)(targ_to_orig_conn_params_ex & 255); /* some sort of identifier of what kind of PLC the target is ??? */
        data[81] = (byte)(targ_to_orig_conn_params_ex >> 8 & 255); /* some sort of identifier of what kind of PLC the target is ??? */
        data[82] = (byte)(targ_to_orig_conn_params_ex >> 16 & 255); /* some sort of identifier of what kind of PLC the target is ??? */
        data[83] = (byte)(targ_to_orig_conn_params_ex >> 24 & 255); /* some sort of identifier of what kind of PLC the target is ??? */
        data[84] = transport_class;        /* ALWAYS 0xA3, server transport, class 3, application trigger */
        data[85] = path_size;              /* size of connection path in 16-bit words
                                     * connection path from MSG instruction.
                                     *
                                     * EG LGX with 1756-ENBT and CPU in slot 0 would be:
                                     * 0x01 - backplane port of 1756-ENBT
                                     * 0x00 - slot 0 for CPU
                                     * 0x20 - class
                                     * 0x02 - MR Message Router
                                     * 0x24 - instance
                                     * 0x01 - instance #1.
                                     */

        return data;
    }

    public override byte[] encodedData()
    {
        throw new NotImplementedException();
    }

    public override void setData(byte[] data)
    {
        //throw new NotImplementedException();
        //        eip_encap e = new eip_encap();
        //Array.Copy(data, 0, this.data, 0, size);
        base.setData(data);

        /*this.encap_command = (UInt16)(data[0] + ((UInt16)data[1] << 8));
        this.encap_length = (UInt16)(data[2] + ((UInt16)data[3] << 8));
        encap_session_handle = (UInt32)(data[4] + ((UInt32)data[5] << 8) + ((UInt32)data[6] << 16) +
            ((UInt32)data[7] << 24));
        encap_status = (UInt32)(data[8] + ((UInt32)data[9] << 8) + ((UInt32)data[10] << 16) +
            ((UInt32)data[11] << 24));
        encap_sender_context = (UInt64)(data[12] + ((UInt64)data[13] << 8) + ((UInt64)data[14] << 16) +
            ((UInt64)data[15] << 24) + ((UInt64)data[16] << 32) + ((UInt64)data[17] << 40) + ((UInt64)data[18] << 48) +
            ((UInt64)data[19] << 56));
        encap_options = (UInt32)(data[20] + ((UInt32)data[21] << 8) + ((UInt32)data[22] << 16) +
            ((UInt32)data[23] << 24));*/


        /* Interface Handle etc. */

        interface_handle = (UInt32)(data[24] + ((UInt32)data[25] << 8) + ((UInt32)data[26] << 16) +
            ((UInt32)data[27] << 24));       /* ALWAYS 0 */
        router_timeout = (UInt16)(data[28] + ((UInt16)data[29] << 8));        /* in seconds */

        /* Common Packet Format - CPF Unconnected */
        cpf_item_count = (UInt16)(data[30] + ((UInt16)data[31] << 8));        /* ALWAYS 2 */
        cpf_nai_item_type = (UInt16)(data[32] + ((UInt16)data[33] << 8));     /* ALWAYS 0 */
        cpf_nai_item_length = (UInt16)(data[34] + ((UInt16)data[35] << 8));   /* ALWAYS 0 */
        cpf_udi_item_type = (UInt16)(data[36] + ((UInt16)data[37] << 8));     /* ALWAYS 0x00B2 - Unconnected Data Item */
        cpf_udi_item_length = (UInt16)(data[38] + ((UInt16)data[39] << 8));   /* REQ: fill in with length of remaining data. */

        /* CM Service Request - Connection Manager */
        cm_service_code = data[40];        /* ALWAYS 0x54 Forward Open Request */
        cm_req_path_size = data[41];       /* ALWAYS 2, size in words of path, next field */
        cm_req_path[0] = data[42];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/
        cm_req_path[1] = data[43];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/
        cm_req_path[2] = data[44];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/
        cm_req_path[3] = data[45];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/

        /* Forward Open Params */
        secs_per_tick = data[46];          /* seconds per tick */
        timeout_ticks = data[47];          /* timeout = srd_secs_per_tick * src_timeout_ticks */
        orig_to_targ_conn_id = (UInt32)(data[48] + ((UInt32)data[49] << 8) + ((UInt32)data[50] << 16) +
            ((UInt32)data[51] << 24));  /* 0, returned by target in reply. */
        targ_to_orig_conn_id = (UInt32)(data[52] + ((UInt32)data[53] << 8) + ((UInt32)data[54] << 16) +
            ((UInt32)data[55] << 24));  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
        conn_serial_number = (UInt16)(data[56] + ((UInt16)data[57] << 8));    /* our connection ID/serial number ?? */
        orig_vendor_id = (UInt16)(data[58] + ((UInt16)data[59] << 8));        /* our unique vendor ID */
        orig_serial_number = (UInt32)(data[60] + ((UInt32)data[61] << 8) + ((UInt32)data[62] << 16) +
            ((UInt32)data[63] << 24));    /* our unique serial number */
        conn_timeout_multiplier = data[64];/* timeout = mult * RPI */
        reserved[0] = data[65];            /* reserved, set to 0 */
        reserved[1] = data[66];            /* reserved, set to 0 */
        reserved[2] = data[67];            /* reserved, set to 0 */
        orig_to_targ_rpi = (UInt32)(data[68] + ((UInt32)data[69] << 8) + ((UInt32)data[70] << 16) +
            ((UInt32)data[71] << 24));      /* us to target RPI - Request Packet Interval in microseconds */
        orig_to_targ_conn_params_ex = (UInt32)(data[72] + ((UInt32)data[73] << 8) + ((UInt32)data[74] << 16) +
            ((UInt32)data[51] << 75)); /* some sort of identifier of what kind of PLC we are??? */
        targ_to_orig_rpi = (UInt32)(data[76] + ((UInt32)data[77] << 8) + ((UInt32)data[78] << 16) +
            ((UInt32)data[79] << 24));      /* target to us RPI, in microseconds */
        targ_to_orig_conn_params_ex = (UInt32)(data[80] + ((UInt32)data[81] << 8) + ((UInt32)data[82] << 16) +
            ((UInt32)data[83] << 24)); /* some sort of identifier of what kind of PLC the target is ??? */
        transport_class = data[84];        /* ALWAYS 0xA3, server transport, class 3, application trigger */
        path_size = data[85];              /* size of connection path in 16-bit words
                                     * connection path from MSG instruction.
                                     *
                                     * EG LGX with 1756-ENBT and CPU in slot 0 would be:
                                     * 0x01 - backplane port of 1756-ENBT
                                     * 0x00 - slot 0 for CPU
                                     * 0x20 - class
                                     * 0x02 - MR Message Router
                                     * 0x24 - instance
                                     * 0x01 - instance #1.
                                     */




        getData();

        //        return e;

        //        throw new NotImplementedException();
    }
    static public eip_encap createFromReq(Request req)
    {
        throw new NotImplementedException();

        //Array.Copy(req.data, this.data, req.data.Length);
        return createFromData(req.data);
    }
    static public eip_encap createFromData(byte[] data)
    {
        throw new NotImplementedException();

        eip_encap res = new eip_encap();

        //Array.Copy(data, res.data, data.Length);
        res.setData(data);
        return res;
    }

}
public class eip_forward_open_response_t : eip_encap
{

    /* Forward Open Response */


    /* encap header */
    /*uint16_le encap_command;    /* ALWAYS 0x006f Unconnected Send*/
    /*uint16_le encap_length;   /* packet size in bytes - 24 */
    /*uint32_le encap_session_handle;  /* from session set up */
    /*uint32_le encap_status;          /* always _sent_ as 0 */
    /*uint64_le encap_sender_context;/* whatever we want to set this to, used for
                                 * identifying responses when more than one
                                 * are in flight at once.
                                 */
    /*uint32_le options;               /* 0, reserved for future use */

    /* Interface Handle etc. */
    public UInt32 interface_handle;      /* ALWAYS 0 */
    public UInt16 router_timeout;        /* in seconds */

    /* Common Packet Format - CPF Unconnected */
    public UInt16 cpf_item_count;        /* ALWAYS 2 */
    public UInt16 cpf_nai_item_type;     /* ALWAYS 0 */
    public UInt16 cpf_nai_item_length;   /* ALWAYS 0 */
    public UInt16 cpf_udi_item_type;     /* ALWAYS 0x00B2 - Unconnected Data Item */
    public UInt16 cpf_udi_item_length;   /* REQ: fill in with length of remaining data. */

    /* Forward Open Reply */
    public byte resp_service_code;      /* returned as 0xD4 or 0xDB */
    public byte reserved1;               /* returned as 0x00? */
    public byte general_status;         /* 0 on success */
    public byte status_size;            /* number of 16-bit words of extra status, 0 if success */
    public byte[] status;
    public UInt32 orig_to_targ_conn_id;  /* target's connection ID for us, save this. */
    public UInt32 targ_to_orig_conn_id;  /* our connection ID back for reference */
    public UInt16 conn_serial_number;    /* our connection ID/serial number from request */
    public UInt16 orig_vendor_id;        /* our unique vendor ID from request*/
    public UInt32 orig_serial_number;    /* our unique serial number from request*/
    public UInt32 orig_to_targ_api;      /* Actual packet interval, microsecs */
    public UInt32 targ_to_orig_api;      /* Actual packet interval, microsecs */
    public byte app_data_size;          /* size in 16-bit words of send_data at end */
    public byte reserved2;
    //uint8_t app_data[ZLA_SIZE];


    public eip_forward_open_response_t() : base() //uint8_t conn_path[ZLA_SIZE];    /* connection path as above */
    {
        /*public const byte*/
        size += 46;
        data = new byte[size];
        for (int i = 0; i < size; i++)
        {
            data[i] = 0;
        }
        //Array.Fill<byte>(data, 0);
        status = new byte[0];

    }

     override public byte[] getData()
    {
        base.getData();

        data[24] = (byte)(interface_handle & 255);
        data[25] = (byte)(interface_handle >> 8 & 255);
        data[26] = (byte)(interface_handle >> 16 & 255);
        data[27] = (byte)(interface_handle >> 24 & 255);
        data[28] = (byte)(router_timeout & 255);        /* in seconds */
        data[29] = (byte)(router_timeout >> 8 & 255);        /* in seconds */
        data[30] = (byte)(cpf_item_count & 255);        /* ALWAYS 2 */
        data[31] = (byte)(cpf_item_count >> 8 & 255);        /* ALWAYS 2 */
        data[32] = (byte)(cpf_nai_item_type & 255);     /* ALWAYS 0 */
        data[33] = (byte)(cpf_nai_item_type >> 8 & 255);     /* ALWAYS 0 */
        data[34] = (byte)(cpf_nai_item_length & 255);   /* ALWAYS 0 */
        data[35] = (byte)(cpf_nai_item_length >> 8 & 255);   /* ALWAYS 0 */
        data[36] = (byte)(cpf_udi_item_type & 255);     /* ALWAYS 0x00B2 - Unconnected Data Item */
        data[37] = (byte)(cpf_udi_item_type >> 8 & 255);     /* ALWAYS 0x00B2 - Unconnected Data Item */
        data[38] = (byte)(cpf_udi_item_length & 255);   /* REQ: fill in with length of remaining data. */
        data[39] = (byte)(cpf_udi_item_length >> 8 & 255);   /* REQ: fill in with length of remaining data. */

        /* CM Service Request - Connection Manager */
        data[40] = resp_service_code;
        data[41] = reserved1;
        data[42] = general_status;
        data[43] = status_size;

        Array.Copy(status, 0, data, 44, status_size);

        data[44+ status_size] = (byte)(orig_to_targ_conn_id & 255);  /* 0, returned by target in reply. */
        data[45 + status_size] = (byte)(orig_to_targ_conn_id >> 8 & 255);  /* 0, returned by target in reply. */
        data[46 + status_size] = (byte)(orig_to_targ_conn_id >> 16 & 255);  /* 0, returned by target in reply. */
        data[47 + status_size] = (byte)(orig_to_targ_conn_id >> 24 & 255);  /* 0, returned by target in reply. */
        data[48 + status_size] = (byte)(targ_to_orig_conn_id & 255);  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
        data[49 + status_size] = (byte)(targ_to_orig_conn_id >> 8 & 255);  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
        data[50 + status_size] = (byte)(targ_to_orig_conn_id >> 16 & 255);  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
        data[51 + status_size] = (byte)(targ_to_orig_conn_id >> 24 & 255);  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
        data[52 + status_size] = (byte)(conn_serial_number & 255);    /* our connection ID/serial number ?? */
        data[53 + status_size] = (byte)(conn_serial_number >> 8 & 255);    /* our connection ID/serial number ?? */
        data[54 + status_size] = (byte)(orig_vendor_id & 255);        /* our unique vendor ID */
        data[55 + status_size] = (byte)(orig_vendor_id >> 8 & 255);        /* our unique vendor ID */
        data[56 + status_size] = (byte)(orig_serial_number & 255);    /* our unique serial number */
        data[57 + status_size] = (byte)(orig_serial_number >> 8 & 255);    /* our unique serial number */
        data[58 + status_size] = (byte)(orig_serial_number >> 16 & 255);    /* our unique serial number */
        data[59 + status_size] = (byte)(orig_serial_number >> 24 & 255);    /* our unique serial number */
        data[60 + status_size] = (byte)(orig_to_targ_api & 255);      /* us to target RPI - Request Packet Interval in microseconds */
        data[61 + status_size] = (byte)(orig_to_targ_api >> 8 & 255);      /* us to target RPI - Request Packet Interval in microseconds */
        data[62 + status_size] = (byte)(orig_to_targ_api >> 16 & 255);      /* us to target RPI - Request Packet Interval in microseconds */
        data[63 + status_size] = (byte)(orig_to_targ_api >> 24 & 255);      /* us to target RPI - Request Packet Interval in microseconds */
        data[64 + status_size] = (byte)(targ_to_orig_api & 255);      /* target to us RPI, in microseconds */
        data[65 + status_size] = (byte)(targ_to_orig_api >> 8 & 255);      /* target to us RPI, in microseconds */
        data[66 + status_size] = (byte)(targ_to_orig_api >> 16 & 255);      /* target to us RPI, in microseconds */
        data[67 + status_size] = (byte)(targ_to_orig_api >> 24 & 255);      /* target to us RPI, in microseconds */
        data[68 + status_size] = app_data_size;
        data[69 + status_size] = reserved2;

        return data;
    }

    public override byte[] encodedData()
    {
        throw new NotImplementedException();
    }

    public override void setData(byte[] data)
    {
        //        eip_encap e = new eip_encap();
        //Array.Copy(data, 0, this.data, 0, size);
        size += 46+status_size;
        this.data = new byte[size+status_size];
        //Array.Fill<byte>(data, 0);

        base.setData(data);

        /*this.encap_command = (UInt16)(data[0] + ((UInt16)data[1] << 8));
        this.encap_length = (UInt16)(data[2] + ((UInt16)data[3] << 8));
        encap_session_handle = (UInt32)(data[4] + ((UInt32)data[5] << 8) + ((UInt32)data[6] << 16) +
            ((UInt32)data[7] << 24));
        encap_status = (UInt32)(data[8] + ((UInt32)data[9] << 8) + ((UInt32)data[10] << 16) +
            ((UInt32)data[11] << 24));
        encap_sender_context = (UInt64)(data[12] + ((UInt64)data[13] << 8) + ((UInt64)data[14] << 16) +
            ((UInt64)data[15] << 24) + ((UInt64)data[16] << 32) + ((UInt64)data[17] << 40) + ((UInt64)data[18] << 48) +
            ((UInt64)data[19] << 56));
        encap_options = (UInt32)(data[20] + ((UInt32)data[21] << 8) + ((UInt32)data[22] << 16) +
            ((UInt32)data[23] << 24));*/


        /* Interface Handle etc. */

        interface_handle = (UInt32)(data[24] + ((UInt32)data[25] << 8) + ((UInt32)data[26] << 16) +
            ((UInt32)data[27] << 24));       /* ALWAYS 0 */
        router_timeout = (UInt16)(data[28] + ((UInt16)data[29] << 8));        /* in seconds */

        /* Common Packet Format - CPF Unconnected */
        cpf_item_count = (UInt16)(data[30] + ((UInt16)data[31] << 8));        /* ALWAYS 2 */
        cpf_nai_item_type = (UInt16)(data[32] + ((UInt16)data[33] << 8));     /* ALWAYS 0 */
        cpf_nai_item_length = (UInt16)(data[34] + ((UInt16)data[35] << 8));   /* ALWAYS 0 */
        cpf_udi_item_type = (UInt16)(data[36] + ((UInt16)data[37] << 8));     /* ALWAYS 0x00B2 - Unconnected Data Item */
        cpf_udi_item_length = (UInt16)(data[38] + ((UInt16)data[39] << 8));   /* REQ: fill in with length of remaining data. */

        /* CM Service Request - Connection Manager */
        resp_service_code = data[40];        /* ALWAYS 0x54 Forward Open Request */
        reserved1 = data[41];       /* ALWAYS 2, size in words of path, next field */
        general_status = data[42];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/
        status_size = data[43];         /* ALWAYS 0x20,0x06,0x24,0x01 for CM, instance 1*/
        status = new byte[status_size];
        Array.Copy(data, 44, status, 0, status_size);
        orig_to_targ_conn_id = (UInt32)(data[44 + status_size] + ((UInt32)data[45 + status_size] << 8) + ((UInt32)data[46 + status_size] << 16) +
            ((UInt32)data[47 + status_size] << 24));  /* 0, returned by target in reply. */
        targ_to_orig_conn_id = (UInt32)(data[48 + status_size] + ((UInt32)data[49 + status_size] << 8) + ((UInt32)data[50 + status_size] << 16) +
            ((UInt32)data[51 + status_size] << 24));  /* what is _our_ ID for this connection, use ab_connection ptr as id ? */
        conn_serial_number = (UInt16)(data[52 + status_size] + ((UInt16)data[53 + status_size] << 8));    /* our connection ID/serial number ?? */
        orig_vendor_id = (UInt16)(data[54 + status_size] + ((UInt16)data[55 + status_size] << 8));        /* our unique vendor ID */
        orig_serial_number = (UInt32)(data[56 + status_size] + ((UInt32)data[57 + status_size] << 8) + ((UInt32)data[58 + status_size] << 16) +
            ((UInt32)data[59 + status_size] << 24));    /* our unique serial number */
        orig_to_targ_api = (UInt32)(data[60 + status_size] + ((UInt32)data[61 + status_size] << 8) + ((UInt32)data[62 + status_size] << 16) +
            ((UInt32)data[63 + status_size] << 24));      /* us to target RPI - Request Packet Interval in microseconds */
        targ_to_orig_api = (UInt32)(data[64 + status_size] + ((UInt32)data[65 + status_size] << 8) + ((UInt32)data[66 + status_size] << 16) +
            ((UInt32)data[67 + status_size] << 24));      /* target to us RPI, in microseconds */
        app_data_size = data[68 + status_size];        /* ALWAYS 0xA3, server transport, class 3, application trigger */
        reserved2 = data[69 + status_size];


        getData();

        //        return e;

        //        throw new NotImplementedException();
    }
    static public eip_encap createFromReq(Request req)
    {
        throw new NotImplementedException();

        //Array.Copy(req.data, this.data, req.data.Length);
        return createFromData(req.data);
    }
    static public eip_encap createFromData(byte[] data)
    {
        throw new NotImplementedException();

        eip_encap res = new eip_encap();

        //Array.Copy(data, res.data, data.Length);
        res.setData(data);
        return res;
    }


}


public class eip_cip_co_resp : eip_encap
{

    /* CIP Response */
    /* encap header */
    //uint16_le encap_command;    /* ALWAYS 0x0070 Connected Send */
    //uint16_le encap_length;   /* packet size in bytes less the header size, which is 24 bytes */
    //uint32_le encap_session_handle;  /* from session set up */
    //uint32_le encap_status;          /* always _sent_ as 0 */
    //uint64_le encap_sender_context;/* whatever we want to set this to, used for
    //                                 * identifying responses when more than one
    //                                 * are in flight at once.
    //                                 */
    //uint32_le options;               /* 0, reserved for future use */

    /* Interface Handle etc. */
    public UInt32 interface_handle;      /* ALWAYS 0 */
    public UInt16 router_timeout;        /* in seconds, zero for Connected Sends! */

    /* Common Packet Format - CPF Connected */
    public UInt16 cpf_item_count;        /* ALWAYS 2 */
    public UInt16 cpf_cai_item_type;     /* ALWAYS 0x00A1 Connected Address Item */
    public UInt16 cpf_cai_item_length;   /* ALWAYS 2 ? */
    public UInt32 cpf_orig_conn_id;      /* our connection ID, NOT the target's */
    public UInt16 cpf_cdi_item_type;     /* ALWAYS 0x00B1, Connected Data Item type */
    public UInt16 cpf_cdi_item_length;   /* length in bytes of the rest of the packet */

    /* connection ID from request */
    public UInt16 cpf_conn_seq_num;      /* connection sequence ID, inc for each message */

    /* CIP Reply */
    public Byte reply_service;          /* 0xCC CIP READ Reply */
    public Byte reserved;               /* 0x00 in reply */
    public Byte status;                 /* 0x00 for success */
    public Byte num_status_words;       /* number of 16-bit words in status */

    /* CIP Data*/
    //uint8_t resp_data[ZLA_SIZE];


    public byte[] data = new byte[50];

    public int size = 50;
    static public int base_size = 50;

    public eip_cip_co_resp() : base() //uint8_t conn_path[ZLA_SIZE];    /* connection path as above */
    {
        /*public const byte*/
        size += 26; // 50;
        data = new byte[size];
        for (int i = 0; i < size; i++)
        {
            data[i] = 0;
        }
        //Array.Fill<byte>(data, 0);
    }

    override public byte[] getData()
    {
        base.getData();

        data[24] = (byte)(interface_handle & 255);
        data[25] = (byte)(interface_handle >> 8 & 255);
        data[26] = (byte)(interface_handle >> 16 & 255);
        data[27] = (byte)(interface_handle >> 24 & 255);
        data[28] = (byte)(router_timeout & 255);        /* in seconds */
        data[29] = (byte)(router_timeout >> 8 & 255);        /* in seconds */
        data[30] = (byte)(cpf_item_count & 255);        /* ALWAYS 2 */
        data[31] = (byte)(cpf_item_count >> 8 & 255);        /* ALWAYS 2 */
        data[32] = (byte)(cpf_cai_item_type & 255);     /* ALWAYS 0 */
        data[33] = (byte)(cpf_cai_item_type >> 8 & 255);     /* ALWAYS 0 */
        data[34] = (byte)(cpf_cai_item_length & 255);   /* ALWAYS 0 */
        data[35] = (byte)(cpf_cai_item_length >> 8 & 255);   /* ALWAYS 0 */
        data[36] = (byte)(cpf_orig_conn_id & 255);      /* our connection ID, NOT the target's */
        data[37] = (byte)(cpf_orig_conn_id >> 8 & 255);      /* our connection ID, NOT the target's */
        data[38] = (byte)(cpf_orig_conn_id >> 16 & 255);      /* our connection ID, NOT the target's */
        data[39] = (byte)(cpf_orig_conn_id >> 24 & 255);      /* our connection ID, NOT the target's */
        data[40] = (byte)(cpf_cdi_item_type & 255);     /* ALWAYS 0x00B1, Connected Data Item type */
        data[41] = (byte)(cpf_cdi_item_type >> 8 & 255);     /* ALWAYS 0x00B1, Connected Data Item type */
        data[42] = (byte)(cpf_cdi_item_length & 255);   /* length in bytes of the rest of the packet */
        data[43] = (byte)(cpf_cdi_item_length >> 8 & 255);   /* length in bytes of the rest of the packet */

        /* connection ID from request */
        data[44] = (byte)(cpf_conn_seq_num & 255);      /* connection sequence ID, inc for each message */
        data[45] = (byte)(cpf_conn_seq_num >> 8 & 255);      /* connection sequence ID, inc for each message */

        /* CIP Reply */
        data[46] = reply_service;          /* 0xCC CIP READ Reply */
        data[47] = reserved;               /* 0x00 in reply */
        data[48] = status;                 /* 0x00 for success */
        data[49] = num_status_words;       /* number of 16-bit words in status */

        return data;
    }

    public override byte[] encodedData()
    {
        throw new NotImplementedException();
    }

    public override void setData(byte[] data)
    {
        //        eip_encap e = new eip_encap();
        //Array.Copy(data, 0, this.data, 0, size);
      //  size += 26; // 50
      //  this.data = new byte[size];
        //Array.Fill<byte>(data, 0);

        base.setData(data);

        /*this.encap_command = (UInt16)(data[0] + ((UInt16)data[1] << 8));
        this.encap_length = (UInt16)(data[2] + ((UInt16)data[3] << 8));
        encap_session_handle = (UInt32)(data[4] + ((UInt32)data[5] << 8) + ((UInt32)data[6] << 16) +
            ((UInt32)data[7] << 24));
        encap_status = (UInt32)(data[8] + ((UInt32)data[9] << 8) + ((UInt32)data[10] << 16) +
            ((UInt32)data[11] << 24));
        encap_sender_context = (UInt64)(data[12] + ((UInt64)data[13] << 8) + ((UInt64)data[14] << 16) +
            ((UInt64)data[15] << 24) + ((UInt64)data[16] << 32) + ((UInt64)data[17] << 40) + ((UInt64)data[18] << 48) +
            ((UInt64)data[19] << 56));
        encap_options = (UInt32)(data[20] + ((UInt32)data[21] << 8) + ((UInt32)data[22] << 16) +
            ((UInt32)data[23] << 24));*/


        /* Interface Handle etc. */

        interface_handle = (UInt32)(data[24] + ((UInt32)data[25] << 8) + ((UInt32)data[26] << 16) +
            ((UInt32)data[27] << 24));       /* ALWAYS 0 */
        router_timeout = (UInt16)(data[28] + ((UInt16)data[29] << 8));        /* in seconds */

        /* Common Packet Format - CPF Unconnected */
        cpf_item_count = (UInt16)(data[30] + ((UInt16)data[31] << 8));        /* ALWAYS 2 */
        cpf_cai_item_type = (UInt16)(data[32] + ((UInt16)data[33] << 8));     /* ALWAYS 0 */
        cpf_cai_item_length = (UInt16)(data[34] + ((UInt16)data[35] << 8));   /* ALWAYS 0 */
        cpf_orig_conn_id = (UInt32)(data[36] + ((UInt32)data[37] << 8) + ((UInt32)data[38] << 16) +
            ((UInt32)data[39] << 24)); ;      /* our connection ID, NOT the target's */
        cpf_cdi_item_type = (UInt16)(data[40] + ((UInt16)data[41] << 8));     /* ALWAYS 0x00B1, Connected Data Item type */
        cpf_cdi_item_length = (UInt16)(data[42] + ((UInt16)data[43] << 8));   /* length in bytes of the rest of the packet */

        /* connection ID from request */
        cpf_conn_seq_num = (UInt16)(data[44] + ((UInt16)data[45] << 8));      /* connection sequence ID, inc for each message */

        /* CIP Reply */
        reply_service = data[46];          /* 0xCC CIP READ Reply */
        reserved = data[47];               /* 0x00 in reply */
        status = data[48];                 /* 0x00 for success */
        num_status_words = data[49];       /* number of 16-bit words in status */

        getData();

        //        return e;

        //        throw new NotImplementedException();
    }
    new static public eip_cip_co_resp createFromReq(Request req)
    {
        throw new NotImplementedException();

        //Array.Copy(req.data, this.data, req.data.Length);
        return createFromData(req.data);
    }
    new static public eip_cip_co_resp createFromData(byte[] data)
    {
        //throw new NotImplementedException();

        eip_cip_co_resp res = new eip_cip_co_resp();

        //Array.Copy(data, res.data, data.Length);
        res.setData(data);
        return res;
    }


}

