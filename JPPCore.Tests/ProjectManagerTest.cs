using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JPP.Core;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace JPPCore.Tests
{
    [TestFixture()]
    class ProjectManagerTest
    {
        [Test]
        [Category("Core")]
        public void IdentifyProjectTest()
        {
            //Test Standard path
            ProjectManager pm = new ProjectManager(false);
            pm.IdentifyProject("M:\\JPP Scheme 2018 (9508- )\\9964C - Bishops Stortford North Phase 2\\Drawings\\JPP Drawings\\test.dwg");
            StringAssert.AreEqualIgnoringCase("9964C", pm.Project);

            //Test non standard path
            pm.IdentifyProject("M:\\JPP Scheme 2018 (9508- )\\10152V - Bishops Stortford North Phase 2\\Drawings\\JPP Drawings\\Additional\\Folders\\Here\\test.dwg");
            StringAssert.AreEqualIgnoringCase("10152V", pm.Project);
        }

    }
}
