// Copyright 2008-2009 Louis DeJardin - http://whereslou.com
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Rhino.Mocks;
using Spark.FileSystem;
using Spark.Web.Mvc.Descriptors;

namespace Spark.Web.Mvc.Tests
{
    [TestFixture]
    public class DescriptorBuildingTester
    {
        private SparkViewFactory _factory;
        private InMemoryViewFolder _viewFolder;
        private RouteData _routeData;
        private ControllerContext _controllerContext;

        [SetUp]
        public void Init()
        {
            CompiledViewHolder.Current = null;

            _factory = new SparkViewFactory();
            _viewFolder = new InMemoryViewFolder();
            _factory.ViewFolder = _viewFolder;

            var httpContext = MockRepository.GenerateStub<HttpContextBase>();
            _routeData = new RouteData();
            var controller = MockRepository.GenerateStub<ControllerBase>();
            _controllerContext = new ControllerContext(httpContext, _routeData, controller);
        }

        private static void AssertDescriptorTemplates(
            SparkViewDescriptor descriptor,
            ICollection<string> searchedLocations,
            params string[] templates)
        {
            Assert.AreEqual(templates.Length, descriptor.Templates.Count, "Descriptor template count must match");
            for (var index = 0; index != templates.Length; ++index)
                Assert.AreEqual(templates[index], descriptor.Templates[index]);
            Assert.AreEqual(0, searchedLocations.Count, "searchedLocations must be empty");
        }

        [Test]
        public void NormalViewAndNoDefaultLayout()
        {
            _routeData.Values.Add("controller", "Home");
            _viewFolder.Add(@"Home\Index.spark", "");

            var searchedLocations = new List<string>();
            var result = _factory.CreateDescriptor(_controllerContext, "Index", null, true, searchedLocations);
            AssertDescriptorTemplates(
                result, searchedLocations,
                @"Home\Index.spark");
        }

        [Test]
        public void NormalViewAndDefaultLayoutPresent()
        {
            _routeData.Values.Add("controller", "Home");
            _viewFolder.Add(@"Home\Index.spark", "");
            _viewFolder.Add(@"Layouts\Application.spark", "");

            var searchedLocations = new List<string>();
            var result = _factory.CreateDescriptor(_controllerContext, "Index", null, true, searchedLocations);
            AssertDescriptorTemplates(
                result, searchedLocations,
                @"Home\Index.spark",
                @"Layouts\Application.spark");
        }


        [Test]
        public void NormalViewAndControllerLayoutOverrides()
        {
            _routeData.Values.Add("controller", "Home");
            _viewFolder.Add(@"Home\Index.spark", "");
            _viewFolder.Add(@"Layouts\Application.spark", "");
            _viewFolder.Add(@"Layouts\Home.spark", "");

            var searchedLocations = new List<string>();
            var result = _factory.CreateDescriptor(_controllerContext, "Index", null, true, searchedLocations);
            AssertDescriptorTemplates(
                result, searchedLocations,
                @"Home\Index.spark",
                @"Layouts\Home.spark");
        }

        [Test]
        public void NormalViewAndNamedMaster()
        {
            _routeData.Values.Add("controller", "Home");
            _viewFolder.Add(@"Home\Index.spark", "");
            _viewFolder.Add(@"Layouts\Application.spark", "");
            _viewFolder.Add(@"Layouts\Home.spark", "");
            _viewFolder.Add(@"Layouts\Site.spark", "");

            var searchedLocations = new List<string>();
            var result = _factory.CreateDescriptor(_controllerContext, "Index", "Site", true, searchedLocations);
            AssertDescriptorTemplates(
                result, searchedLocations,
                @"Home\Index.spark",
                @"Layouts\Site.spark");
        }

        [Test]
        public void PartialViewIgnoresDefaultLayouts()
        {
            _routeData.Values.Add("controller", "Home");
            _viewFolder.Add(@"Home\Index.spark", "");
            _viewFolder.Add(@"Layouts\Application.spark", "");
            _viewFolder.Add(@"Layouts\Home.spark", "");
            _viewFolder.Add(@"Shared\Application.spark", "");
            _viewFolder.Add(@"Shared\Home.spark", "");

            var searchedLocations = new List<string>();
            var result = _factory.CreateDescriptor(_controllerContext, "Index", null, false, searchedLocations);
            AssertDescriptorTemplates(
                result, searchedLocations,
                @"Home\Index.spark");
        }

        [Test]
        public void RouteAreaPresentDefaultsToNormalLocation()
        {
            _routeData.Values.Add("area", "Admin");
            _routeData.Values.Add("controller", "Home");
            _viewFolder.Add(@"Home\Index.spark", "");
            _viewFolder.Add(@"Layouts\Application.spark", "");

            var searchedLocations = new List<string>();
            var result = _factory.CreateDescriptor(_controllerContext, "Index", null, true, searchedLocations);
            AssertDescriptorTemplates(
                result, searchedLocations,
                @"Home\Index.spark",
                @"Layouts\Application.spark");
        }

        [Test]
        public void AreaFolderMayContainControllerFolder()
        {
            _routeData.Values.Add("area", "Admin");
            _routeData.Values.Add("controller", "Home");
            _viewFolder.Add(@"Home\Index.spark", "");
            _viewFolder.Add(@"Layouts\Application.spark", "");
            _viewFolder.Add(@"Admin\Home\Index.spark", "");

            var searchedLocations = new List<string>();
            var result = _factory.CreateDescriptor(_controllerContext, "Index", null, true, searchedLocations);
            AssertDescriptorTemplates(
                result, searchedLocations,
                @"Admin\Home\Index.spark",
                @"Layouts\Application.spark");
        }

        [Test]
        public void AreaFolderMayContainLayoutsFolder()
        {
            _routeData.Values.Add("area", "Admin");
            _routeData.Values.Add("controller", "Home");
            _viewFolder.Add(@"Home\Index.spark", "");
            _viewFolder.Add(@"Layouts\Application.spark", "");
            _viewFolder.Add(@"Admin\Home\Index.spark", "");
            _viewFolder.Add(@"Admin\Layouts\Application.spark", "");

            var searchedLocations = new List<string>();
            var result = _factory.CreateDescriptor(_controllerContext, "Index", null, true, searchedLocations);
            AssertDescriptorTemplates(
                result, searchedLocations,
                @"Admin\Home\Index.spark",
                @"Admin\Layouts\Application.spark");
        }

        [Test]
        public void AreaContainsNamedLayout()
        {
            _routeData.Values.Add("area", "Admin");
            _routeData.Values.Add("controller", "Home");
            _viewFolder.Add(@"Home\Index.spark", "");
            _viewFolder.Add(@"Layouts\Application.spark", "");
            _viewFolder.Add(@"Admin\Home\Index.spark", "");
            _viewFolder.Add(@"Admin\Layouts\Application.spark", "");
            _viewFolder.Add(@"Admin\Layouts\Site.spark", "");

            var searchedLocations = new List<string>();
            var result = _factory.CreateDescriptor(_controllerContext, "Index", "Site", true, searchedLocations);
            AssertDescriptorTemplates(
                result, searchedLocations,
                @"Admin\Home\Index.spark",
                @"Admin\Layouts\Site.spark");
        }

        [Test]
        public void PartialViewFromAreaIgnoresLayout()
        {
            _routeData.Values.Add("area", "Admin");
            _routeData.Values.Add("controller", "Home");
            _viewFolder.Add(@"Home\Index.spark", "");
            _viewFolder.Add(@"Admin\Home\Index.spark", "");
            _viewFolder.Add(@"Layouts\Home.spark", "");
            _viewFolder.Add(@"Layouts\Application.spark", "");
            _viewFolder.Add(@"Admin\Layouts\Application.spark", "");
            _viewFolder.Add(@"Admin\Layouts\Home.spark", "");
            _viewFolder.Add(@"Admin\Layouts\Site.spark", "");

            var searchedLocations = new List<string>();
            var result = _factory.CreateDescriptor(_controllerContext, "Index", null, false, searchedLocations);
            AssertDescriptorTemplates(
                result, searchedLocations,
                @"Admin\Home\Index.spark");
        }

        [Test]
        public void UseMasterCreatesTemplateChain()
        {
            _routeData.Values.Add("controller", "Home");
            _viewFolder.Add(@"Home\Index.spark", "<use master='Green'/>");
            _viewFolder.Add(@"Layouts\Red.spark", "<use master='Blue'/>");
            _viewFolder.Add(@"Layouts\Green.spark", "<use master='Red'/>");
            _viewFolder.Add(@"Layouts\Blue.spark", "");
            _viewFolder.Add(@"Layouts\Application.spark", "");
            _viewFolder.Add(@"Layouts\Home.spark", "");

            var searchedLocations = new List<string>();
            var result = _factory.CreateDescriptor(_controllerContext, "Index", null, true, searchedLocations);
            AssertDescriptorTemplates(
                result, searchedLocations,
                @"Home\Index.spark",
                @"Layouts\Green.spark",
                @"Layouts\Red.spark",
                @"Layouts\Blue.spark");
        }

        [Test]
        public void NamedMasterOverridesViewMaster()
        {
            _routeData.Values.Add("controller", "Home");
            _viewFolder.Add(@"Home\Index.spark", "<use master='Green'/>");
            _viewFolder.Add(@"Layouts\Red.spark", "<use master='Blue'/>");
            _viewFolder.Add(@"Layouts\Green.spark", "<use master='Red'/>");
            _viewFolder.Add(@"Layouts\Blue.spark", "");
            _viewFolder.Add(@"Layouts\Application.spark", "");
            _viewFolder.Add(@"Layouts\Home.spark", "");

            var searchedLocations = new List<string>();
            var result = _factory.CreateDescriptor(_controllerContext, "Index", "Red", true, searchedLocations);
            AssertDescriptorTemplates(
                result, searchedLocations,
                @"Home\Index.spark",
                @"Layouts\Red.spark",
                @"Layouts\Blue.spark");
        }

        [Test]
        public void PartialViewIgnoresUseMasterAndDefault()
        {
            _routeData.Values.Add("controller", "Home");
            _viewFolder.Add(@"Home\Index.spark", "<use master='Green'/>");
            _viewFolder.Add(@"Layouts\Red.spark", "<use master='Blue'/>");
            _viewFolder.Add(@"Layouts\Green.spark", "<use master='Red'/>");
            _viewFolder.Add(@"Layouts\Blue.spark", "");
            _viewFolder.Add(@"Layouts\Application.spark", "");
            _viewFolder.Add(@"Layouts\Home.spark", "");

            var searchedLocations = new List<string>();
            var result = _factory.CreateDescriptor(_controllerContext, "Index", null, false, searchedLocations);
            AssertDescriptorTemplates(
                result, searchedLocations,
                @"Home\Index.spark");
        }

        [Test]
        public void BuildDescriptorParamsActsAsKey()
        {
            var param1 = new BuildDescriptorParams("a", "c", "d", "e", false, null);
            var param2 = new BuildDescriptorParams("a", "c", "d", "e", false, null);
            var param3 = new BuildDescriptorParams("a", "c", "d", "e", true, null);
            var param4 = new BuildDescriptorParams("a", "c2", "d", "e", false, null);

            Assert.That(param1, Is.EqualTo(param2));
            Assert.That(param1, Is.Not.EqualTo(param3));
            Assert.That(param1, Is.Not.EqualTo(param4));
        }

        [Test]
        public void ParamsExtraNullActsAsEmpty()
        {
            var param1 = new BuildDescriptorParams("a", "c", "d", "e", false, null);
            var param2 = new BuildDescriptorParams("a", "c", "d", "e", false, new Dictionary<string, object>());

            Assert.That(param1, Is.EqualTo(param2));
        }

        [Test]
        public void ParamsExtraEqualityMustBeIdentical()
        {
            var param1 = new BuildDescriptorParams("a", "c", "d", "e", false, Dict(new[] { "alpha", "beta" }));
            var param2 = new BuildDescriptorParams("a", "c", "d", "e", false, Dict(new[] { "alpha" }));
            var param3 = new BuildDescriptorParams("a", "c", "d", "e", false, Dict(new[] { "beta" }));
            var param4 = new BuildDescriptorParams("a", "c", "d", "e", false, Dict(new[] { "beta", "alpha" }));
            var param5 = new BuildDescriptorParams("a", "c", "d", "e", false, Dict(null));
            var param6 = new BuildDescriptorParams("a", "c", "d", "e", false, Dict(new string[0]));
            var param7 = new BuildDescriptorParams("a", "c", "d", "e", false, Dict(new[] { "alpha", "beta" }));

            Assert.That(param1, Is.Not.EqualTo(param2));
            Assert.That(param1, Is.Not.EqualTo(param3));
            Assert.That(param1, Is.Not.EqualTo(param4));
            Assert.That(param1, Is.Not.EqualTo(param5));
            Assert.That(param1, Is.Not.EqualTo(param6));
            Assert.That(param1, Is.EqualTo(param7));
        }

        private static IDictionary<string, object> Dict(IEnumerable<string> values)
        {
            return values == null
                       ? null
                       : values
                             .Select((v, k) => new {k, v})
                             .ToDictionary(kv => kv.k.ToString(), kv => (object) kv.v);
        }

        [Test]
        public void CustomParameterAddedToViewSearchPath()
        {
            _factory.DescriptorBuilder = new ExtendingDescriptorBuilderWithInheritance(_factory.Engine);
            _routeData.Values.Add("controller", "Home");
            _routeData.Values.Add("language", "en-us");
            _viewFolder.Add(@"Home\Index.en-us.spark", "");
            _viewFolder.Add(@"Home\Index.en.spark", "");
            _viewFolder.Add(@"Home\Index.spark", "");
            _viewFolder.Add(@"Layouts\Application.en.spark", "");
            _viewFolder.Add(@"Layouts\Application.ru.spark", "");
            _viewFolder.Add(@"Layouts\Application.spark", "");

            var searchedLocations = new List<string>();
            var result = _factory.CreateDescriptor(_controllerContext, "Index", null, true, searchedLocations);
            AssertDescriptorTemplates(
                result, searchedLocations,
                @"Home\Index.en-us.spark",
                @"Layouts\Application.en.spark");
        }

        public class ExtendingDescriptorBuilderWithInheritance : DefaultDescriptorBuilder
        {
            public ExtendingDescriptorBuilderWithInheritance()
            {
            }

            public ExtendingDescriptorBuilderWithInheritance(ISparkViewEngine engine)
                : base(engine)
            {
            }

            public override IDictionary<string, object> GetExtraParameters(ControllerContext controllerContext)
            {
                return new Dictionary<string, object>
                           {
                               {"language", Convert.ToString(controllerContext.RouteData.Values["language"])}
                           };
            }

            protected override IEnumerable<string> PotentialViewLocations(string controllerName, string viewName, IDictionary<string, object> extra)
            {
                return Merge(base.PotentialViewLocations(controllerName, viewName, extra), extra["language"].ToString());
            }

            protected override IEnumerable<string> PotentialMasterLocations(string masterName, IDictionary<string, object> extra)
            {
                return Merge(base.PotentialMasterLocations(masterName, extra), extra["language"].ToString());
            }

            protected override IEnumerable<string> PotentialDefaultMasterLocations(string controllerName, IDictionary<string, object> extra)
            {
                return Merge(base.PotentialDefaultMasterLocations(controllerName, extra), extra["language"].ToString());
            }

            static IEnumerable<string> Merge(IEnumerable<string> locations, string region)
            {
                var slashPos = (region ?? "").IndexOf('-');
                var language = slashPos == -1 ? null : region.Substring(0, slashPos);

                foreach (var location in locations)
                {
                    if (!string.IsNullOrEmpty(region))
                    {
                        yield return Path.ChangeExtension(location, region + ".spark");
                        if (slashPos != -1)
                            yield return Path.ChangeExtension(location, language + ".spark");
                    }
                    yield return location;
                }
            }
        }

        [Test, ExpectedException(typeof(InvalidCastException))]
        public void CustomDescriptorBuildersCantUseDescriptorFilters()
        {
            _factory.DescriptorBuilder = MockRepository.GenerateStub<IDescriptorBuilder>();
            _factory.AddFilter(MockRepository.GenerateStub<IDescriptorFilter>());
        }
    }
}
