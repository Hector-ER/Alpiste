using Alpiste.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks; 

namespace Alpiste.Utils
{
    public class Cond
    {
        /***************************************************************************
 ************************* Condition Variables *****************************
 ***************************************************************************/

        Object /*CRITICAL_SECTION*/ cs;
        bool /*CONDITION_VARIABLE*/ cond;
        int flag;


        public Cond()  // cond_create(cond_p c)
        {
            int rc = Lib.PlcTag.PLCTAG_STATUS_OK;
            //Cond tmp_cond = NULL;

            //pdebug(DEBUG_DETAIL, "Starting.");

            /*if (!c)
            {
                pdebug(DEBUG_WARN, "Null pointer to condition var pointer!");
                return PLCTAG_ERR_NULL_PTR;
            }*/

            /*if (*c)
            {
                pdebug(DEBUG_WARN, "Condition var pointer is not null, was it not deleted first?");
            }*/

            /* clear the output first. */
            //*c = NULL;

            /*tmp_cond = mem_alloc((int)(unsigned int)sizeof(*tmp_cond));
            if (!tmp_cond)
            {
                pdebug(DEBUG_WARN, "Unable to allocate new condition var!");
                return PLCTAG_ERR_NO_MEM;
            }*/

            //InitializeCriticalSection(cs);
            //InitializeConditionVariable(cond);
            cs = new Object();
            cond = false;

            flag = 0;

            //*c = tmp_cond;

            //pdebug(DEBUG_DETAIL, "Done.");

            //return rc;
        }


        public int cond_wait /*_impl*/(/*const char* func, int line_num, cond_p c,*/ int timeout_ms)
        {
            int rc = Lib.PlcTag.PLCTAG_STATUS_OK;
            Int64 start_time = DateTime.Now.Millisecond; // time_ms();

            //pdebug(DEBUG_SPEW, "Starting. Called from %s:%d.", func, line_num);

            /*if (c == null) {
                //pdebug(DEBUG_WARN, "Condition var pointer is null in call from %s:%d!", func, line_num);
                return Lib.PlcTag.PLCTAG_ERR_NULL_PTR;
            }*/

            if (timeout_ms <= 0) {
                //pdebug(DEBUG_WARN, "Timeout must be a positive value but was %d in call from %s:%d!", timeout_ms, func, line_num);
                return Lib.PlcTag.PLCTAG_ERR_BAD_PARAM;
            }


            //EnterCriticalSection(&(c->cs));
            lock (cs)
            {
                while (flag == 0)
                {
                    Int64 time_left = (Int64)timeout_ms - (DateTime.Now.Millisecond /*time_ms()*/ - start_time);

                    if (time_left > 0)
                    {
                        int wait_rc = 0;

                        //if (!cond)
                            if (Monitor.Wait(cs, (int) time_left))
                                
                        /*if (!cond)
                             { Monitor.Wait(cs, time_left); } 
                        */
                        //Monitor.Wait(cond, (int) time_left);

                        /*if (SleepConditionVariableCS(&(c->cond), &(c->cs), (DWORD)time_left))
                        {
                            /* we might need to wait again. could be a spurious wake up. */
                            //pdebug(DEBUG_SPEW, "Condition var wait returned.");
                            rc = Lib.PlcTag.PLCTAG_STATUS_OK;
                        //}
                        /*else 
                        {
                            /* error or timeout. */
                        /*    wait_rc = GetLastError();
                            if (wait_rc == ERROR_TIMEOUT)
                            {
                                pdebug(DEBUG_SPEW, "Timeout response from condition var wait.");
                                rc = PLCTAG_ERR_TIMEOUT;
                                break;
                            }
                            else
                            {
                                pdebug(DEBUG_WARN, "Error %d waiting on condition variable!", wait_rc);
                                rc = PLCTAG_ERR_BAD_STATUS;
                                break;
                            }
                        }*/
                    }
                    else
                    {
                        //pdebug(DEBUG_SPEW, "Timed out.");
                        rc = Lib.PlcTag.PLCTAG_ERR_TIMEOUT;
                        break;
                    }
                }

                /*if (c->flag)
                {
                    pdebug(DEBUG_SPEW, "Condition var signaled for call at %s:%d.", func, line_num);

                    /* clear the flag now that we've responded. */
                /*    c->flag = 0;
                }*/
                /*else
                {
                    pdebug(DEBUG_SPEW, "Condition wait terminated due to error or timeout for call at %s:%d.", func, line_num);
                }*/


            }
            //LeaveCriticalSection(&(c->cs));

            //pdebug(DEBUG_SPEW, "Done for call at %s:%d.", func, line_num);
    
            return rc;
        }


        public int cond_signal/*_impl*/(/*const char* func, int line_num, cond_p c*/ /*Cond c*/)
        {
            int rc = Lib.PlcTag.PLCTAG_STATUS_OK;

                //pdebug(DEBUG_SPEW, "Starting.  Called from %s:%d.", func, line_num);

               /* if (c != null)
                {
                    //pdebug(DEBUG_WARN, "Condition var pointer is null in call at %s:%d!", func, line_num);
                    return Lib.PlcTag.PLCTAG_ERR_NULL_PTR;
                }*/

                //EnterCriticalSection(&(c->cs));

            lock (cs)
            {

                flag = 1;
                    Monitor.Pulse(cs);
            }
                //LeaveCriticalSection(&(c->cs));

            /* Windows does this outside the critical section? */
            //WakeConditionVariable(&(c->cond));

            //pdebug(DEBUG_SPEW, "Done for call at %s:%d.", func, line_num);
            return rc;
        }



        public int cond_clear/*_impl*/(/*const char* func, int line_num, cond_p c*/)
        {
            int rc = Lib.PlcTag.PLCTAG_STATUS_OK;

            //pdebug(DEBUG_SPEW, "Starting.  Called from %s:%d.", func, line_num);

            /*if (!c)
            {
                //pdebug(DEBUG_WARN, "Condition var pointer is null in call at %s:%d!", func, line_num);
                return PLCTAG_ERR_NULL_PTR;
            }*/

            //EnterCriticalSection(&(c->cs));
            lock(cs)
                flag = 0;

            //LeaveCriticalSection(&(c->cs));

            //pdebug(DEBUG_SPEW, "Done for call at %s:%d.", func, line_num);
            return rc;
        }


        int cond_destroy(/*cond_p* c*/)
        {
            int rc = Lib.PlcTag.PLCTAG_STATUS_OK;

            //pdebug(DEBUG_DETAIL, "Starting.");

            /*if (!c || !*c)
            {
                pdebug(DEBUG_WARN, "Condition var pointer is null!");
                return PLCTAG_ERR_NULL_PTR;
            }*/

            /*    mem_free(*c);

                *c = NULL;

            pdebug(DEBUG_DETAIL, "Done.");*/

            return rc;
        }




    }
}
