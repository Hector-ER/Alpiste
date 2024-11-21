using Alpiste.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Alpiste.Utils
{
    public class attr_entry
    {
        public attr_entry next;
        public String name;
        public String val;
    };

    public class attr
    {
        public attr_entry head;
        public attr()
        {
            // return (attr)mem_alloc(sizeof(struct attr_t));
        }

    };

    public class Attr
    {

        /*
         * attr_create_from_str
         *
         * Parse the passed string into an attr structure and return a pointer to a list of
         * attr_entry structs.
         *
         * Attribute strings are formatted much like URL arguments:
         * foo=bar&blah=humbug&blorg=42&test=one
         * You cannot, currently, have an "=" or "&" character in the value for an 
         * attribute.
         */
        public static attr attr_create_from_str(String attr_str)
        {
            attr res = null;
            String[] kv_pairs = null;

            //pdebug(DEBUG_DETAIL, "Starting.");

            if (attr_str.Length == 0) {
                //pdebug(DEBUG_WARN, "Attribute string needs to be longer than zero characters!");
                return null;
            }

            /* split the string on "&" */
            //kv_pairs = attr_str.Split("&");
            kv_pairs = attr_str.Split('&');
            if (kv_pairs.Length == 0) {
                //pdebug(DEBUG_WARN, "No key-value pairs!");
                return null;
            }

            /* set up the attribute list head */
            res = new attr(); // attr_create();
                              //if (!res)
                              //{
                              //  pdebug(DEBUG_ERROR, "Unable to allocate memory for attribute list!");
                              //  mem_free(kv_pairs);
                              //  return NULL;
                              //}

            /* loop over each key-value pair */
            foreach (String kv_pair in kv_pairs)
            //for (char** kv_pair = kv_pairs; *kv_pair; kv_pair++)
            {
                /* find the position of the '=' character */
                int separator = kv_pair.IndexOf("="); // strchr(*kv_pair, '=');
                String key = kv_pair.Substring(0, separator); //*kv_pair;
                String value = separator == -1 ? "" : kv_pair.Substring(separator + 1);

                //pdebug(DEBUG_DETAIL, "Key-value pair \"%s\".", *kv_pair);

                if (separator == -1)
                {
                    //pdebug(DEBUG_WARN, "Attribute string \"%s\" has invalid key-value pair near \"%s\"!", attr_str, *kv_pair);
                    //mem_free(kv_pairs);
                    //attr_destroy(res);
                    return null;
                }

                /* value points to the '=' character.  Step past that for the value. */
                //value++;

                /* cut the string at the separator. */
                //*separator = (char)0;

                //pdebug(DEBUG_DETAIL, "Key-value pair before trimming \"%s\":\"%s\".", key, value);

                /* skip leading spaces in the key */
                //while (*key == ' ')
                //{
                //    key++;
                //}
                key= key.Trim();

                /* zero out all trailing spaces in the key */
                //for (int i = str_length(key) - 1; i > 0 && key[i] == ' '; i--)
                //{
                //    key[i] = (char)0;
                //}

                //pdebug(DEBUG_DETAIL, "Key-value pair after trimming \"%s\":\"%s\".", key, value);

                /* check the string lengths */

                if (key.Length <= 0)
                {
                    //pdebug(DEBUG_WARN, "Attribute string \"%s\" has invalid key-value pair near \"%s\"!  Key must not be zero length!", attr_str, *kv_pair);
                    //mem_free(kv_pairs);
                    //attr_destroy(res);
                    return null;
                }

                if (value.Length <= 0)
                {
                    //pdebug(DEBUG_WARN, "Attribute string \"%s\" has invalid key-value pair near \"%s\"!  Value must not be zero length!", attr_str, *kv_pair);
                    //mem_free(kv_pairs);
                    //attr_destroy(res);
                    return null;
                }

                value = value.Trim();
                /* add the key-value pair to the attribute list */
                if (attr_set_str(ref res, key, value) != 0)
                {
                    //pdebug(DEBUG_WARN, "Unable to add key-value pair \"%s\":\"%s\" to attribute list!", key, value);
                    //mem_free(kv_pairs);
                    //attr_destroy(res);
                    return null;
                }
            }

            //if (kv_pairs)
            //{
            //    mem_free(kv_pairs);
            //}

            //pdebug(DEBUG_DETAIL, "Done.");

            return res;
        }

        /*
         * attr_set
         *
         * Set/create a new string attribute
         */
        static public int attr_set_str(ref attr attrs, String name, String val)
        {
            attr_entry e;

            if (attrs == null) {
                return 1;
            }

            /* does the entry exist? */
            e = find_entry(attrs, name);

            /* if we had a match, then delete the existing value and add in the
             * new one.
             *
             * If we had no match, then e is NULL and we need to create a new one.
             */
            if (e != null) {
                /* we had a match, free any existing value */
                if (e.val != null) {
                    //mem_free(e->val);
                }

                /* set up the new value */
                e.val = val; // str_dup(val);
                if (e.val == null) {
                    /* oops! */
                    return 1;
                }
            } else
            {
                /* no match, need a new entry */
                //e = (attr_entry)mem_alloc(sizeof(struct attr_entry_t));
                e = new attr_entry();

                if (e != null)
                {
                    e.name = name; // str_dup(name);

                    if (e.name == null)
                    {
                        e = null;//mem_free(e);
                        return 1;
                    }

                    e.val = val; // str_dup(val);

                    if (e.val == null)
                    {
                        e.name = null; //mem_free(e->name);
                        e = null; // mem_free(e);
                        return 1;
                    }

                    /* link it in the list */
                    e.next = attrs.head;
                    attrs.head = e;
                }
                else
                {
                    /* allocation failed */
                    return 1;
                }
            }

            return 0;
        }

        /*
         * find_entry
         *
         * A helper function to find the attr_entry that has the
         * passed name.
         */

        static attr_entry find_entry(attr a, String name)
        {
            attr_entry e;

            if (a == null)
                return null;

            e = a.head;

            if (e == null)
                return null;

            while (e != null) {
                if (e.name == name) {
                    return e;
                }

                e = e.next;
            }

            return null;
        }

        /*
         * attr_get
         *
         * Walk the list of attrs and return the value found with the passed name.
         * If the name is not found, return the passed default value.
         */
        static public String attr_get_str(attr attrs, String name, String def)
        {
            attr_entry e;

            if (attrs == null) {
                return def;
            }

            e = find_entry(attrs, name);

            /* only return a value if there is one. */
            if (e != null) {
                return e.val;
            } else
            {
                return def;
            }
        }

        static public int attr_get_int(attr attrs, String name, int def)
        {
            int res;
            int rc;

            String str_val = attr_get_str(attrs, name, null);

            if(str_val == null) {
                return def;
            }

            //rc = str_to_int(str_val, &res);
            res = int.Parse(str_val);

            //if(rc) {
            //    /* format error? */
            //    return def;
            //} else
            //{
                return res;
            //}
        }

        public static int attr_set_int(attr attrs, String name, int val)
        {
            //char buf[64];

            //snprintf_platform(buf, sizeof buf, "%d", val);

            return attr_set_str(ref attrs, name, /*buf*/val.ToString());
        }


    }
}
