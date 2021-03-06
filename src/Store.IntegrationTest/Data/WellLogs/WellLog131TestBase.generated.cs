//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

// ----------------------------------------------------------------------
// <auto-generated>
//     Changes to this file may cause incorrect behavior and will be lost
//     if the code is regenerated.
// </auto-generated>
// ----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.WellLogs
{
    public abstract partial class WellLog131TestBase : IntegrationTestFixtureBase<DevKit131Aspect>
    {
        public const string QueryMissingNamespace = "<wellLogs version=\"1.3.1.1\"><wellLog /></wellLogs>";
        public const string QueryInvalidNamespace = "<wellLogs xmlns=\"www.witsml.org/schemas/123\" version=\"1.3.1.1\"></wellLogs>";
        public const string QueryMissingVersion = "<wellLogs xmlns=\"http://www.witsml.org/schemas/131\"></wellLogs>";
        public const string QueryEmptyRoot = "<wellLogs xmlns=\"http://www.witsml.org/schemas/131\" version=\"1.3.1.1\"></wellLogs>";
        public const string QueryEmptyObject = "<wellLogs xmlns=\"http://www.witsml.org/schemas/131\" version=\"1.3.1.1\"><wellLog /></wellLogs>";
        public const string BasicXMLTemplate = "<wellLogs xmlns=\"http://www.witsml.org/schemas/131\" version=\"1.3.1.1\"><wellLog uidWell=\"{0}\" uidWellbore=\"{1}\" uid=\"{2}\">{3}</wellLog></wellLogs>";

        protected WellLog131TestBase(bool isEtpTest = false)
            : base(isEtpTest)
        {
        }

        public Well Well { get; set; }
        public Wellbore Wellbore { get; set; }
        public WellLog WellLog { get; set; }
        public List<WellLog> QueryEmptyList { get; set; }

        protected override void PrepareData()
        {

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version131.Value)
                .ToArray();

            Well = new Well
            {
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Well"),
                TimeZone = DevKit.TimeZone
            };
            Wellbore = new Wellbore
            {
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Wellbore"),
                UidWell = Well.Uid,
                NameWell = Well.Name,
                MDCurrent = new MeasuredDepthCoord(0, MeasuredDepthUom.ft)
            };
            WellLog = new WellLog
            {
                Uid = DevKit.Uid(),
                Name = DevKit.Name("WellLog"),
                UidWell = Well.Uid,
                NameWell = Well.Name,
                UidWellbore = Wellbore.Uid,
                NameWellbore = Wellbore.Name
            };

            QueryEmptyList = DevKit.List(new WellLog());

        }

        protected virtual void AddParents()
        {
            DevKit.AddAndAssert<WellList, Well>(Well);
            DevKit.AddAndAssert<WellboreList, Wellbore>(Wellbore);
        }
    }
}