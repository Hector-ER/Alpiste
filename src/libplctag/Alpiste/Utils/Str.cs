using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Alpiste.Utils
{
    internal class Str
    {
        /*
        ** Designation:  StriStr
        **
        ** Call syntax:  char *stristr(char *String, char *Pattern)
        **
        ** Description:  This function is an ANSI version of strstr() with
        **               case insensitivity.
        **
        ** Return item:  char *pointer if Pattern is found in String, else
        **               pointer to 0
        **
        ** Rev History:  07/04/95  Bob Stout  ANSI-fy
        **               02/03/94  Fred Cole  Original
        **
        ** Hereby donated to public domain.
        **
        ** Modified for use with libcyrus by Ken Murchison 06/01/00.
        */


        static public String str_str_cmp_i(String haystack, String needle)
        {
            int nptr, hptr, start;
            int haystack_len = haystack.Length;
            int needle_len = needle.Length;

            if (haystack_len==0) {
                //pdebug(DEBUG_DETAIL, "Haystack string is NULL or zero length.");
                return null;
            }

            if (needle_len==0) {
                //pdebug(DEBUG_DETAIL, "Needle string is NULL or zero length.");
                return null;
            }

            if (haystack_len < needle_len)
            {
                //pdebug(DEBUG_DETAIL, "Needle string is longer than haystack string.");
                return null;
            }

            /* while haystack length not shorter than needle length */
            //for (start = haystack, nptr = needle; haystack_len >= needle_len; start++, haystack_len--)
            for (start = 0, nptr = 0; haystack_len >= needle_len; start++, haystack_len--)
            {
                /* find start of needle in haystack */
                //while (toupper(*start) != toupper(*needle))
                while (haystack.ToUpper()[start] == needle.ToUpper()[0])
                {
                    start++;
                    haystack_len--;

                    /* if needle longer than haystack */

                    if (haystack_len < needle_len)
                    {
                        return (null);
                    }
                }

                hptr = start;
                nptr = 0; // (char*)needle;

                //while (toupper(*hptr) == toupper(*nptr))
                while (haystack.ToUpper()[hptr] == needle.ToUpper()[nptr])
                {
                    hptr++;
                    nptr++;

                    /* if end of needle then needle was found */
                    //if ('\0' == *nptr)
                    if (nptr == needle_len)
                    {
                        return haystack.Substring(start); //(start);
                    }
                }
            }

            return (null);
        }

        /*
         * str_to_int
         *
         * Convert the characters in the passed string into
         * an int.  Return an int in integer in the passed
         * pointer and a status from the function.
         */
        public static int str_to_int(string cadena, ref int numero)
        {
            int indice = 0;
            int i = 0;
            bool negativo = false;

            while (i < cadena.Length && char.IsWhiteSpace(cadena[i]))
            {
                i++;
            }
            if (i < cadena.Length && cadena[i] == '-') {
                negativo = true;
                i++;
            }
            else if(i < cadena.Length && cadena[i] == '+')
            {
                i++;
            }
            while (i < cadena.Length && char.IsDigit(cadena[i])) {
                numero = numero * 10 + cadena[i] - '0';
                i++;
            }
            indice = i;

            return 0; // negativo ? -numero : numero;
        }
        public static int str_to_int_(String /*const char**/ str, ref int val) 
        {
            int /*char**/ endptr;
            long /* int*/ tmp_val;

            //tmp_val = strtol(str,&endptr,0);
            val = int.Parse(str);
            /*if (errno == ERANGE && (tmp_val == LONG_MAX || tmp_val == LONG_MIN)) {
                /*pdebug("strtol returned %ld with errno %d",tmp_val, errno);*/
            /*     return -1;
            }

            if (endptr == str) {
                return -1;
            }

            /* FIXME - this will truncate long values. */
            //*val = (int) tmp_val;

            return 0;
        }
    
    }
}
