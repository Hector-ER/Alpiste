using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Alpiste.Lib;

namespace libplctag.Tests
{

    public  class AplisteTests
    {

        [Fact]
        public void Contructor_Destructor()
        {
            PlcTag t1 = new PlcTag();
            PlcTag t2 = new PlcTag();
            PlcTag t3 = new PlcTag();
            Assert.Equal(PlcTag.tags.Count, 0);
            Assert.True(PlcTag.tag_tickler_thread.IsAlive);
            t1.Dispose();
            Assert.Equal(PlcTag.tags.Count, 2);
            Assert.True(PlcTag.tag_tickler_thread.IsAlive);
            t2.Dispose();
            Assert.Equal(PlcTag.tags.Count, 1);
            Assert.True(PlcTag.tag_tickler_thread.IsAlive);
            t3.Dispose();
            Assert.Equal(PlcTag.tags.Count, 0);
            Assert.False(PlcTag.tag_tickler_thread.IsAlive);

        }
    }
}
