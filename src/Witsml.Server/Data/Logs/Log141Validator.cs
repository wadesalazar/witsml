﻿using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Energistics.DataAccess.WITSML141;
using System.ComponentModel.Composition;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Provides validation for <see cref="Log" /> data objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.DataObjectValidator{Energistics.DataAccess.WITSML141.Log}" />
    [Export(typeof(IDataObjectValidator<Log>))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class Log141Validator : DataObjectValidator<Log>
    {
        private readonly IWitsmlDataAdapter<Log> _logDataAdapter;
        private readonly IWitsmlDataAdapter<Wellbore> _wellboreDataAdapter;
        private readonly IWitsmlDataAdapter<Well> _wellDataAdapter;

        // TODO: Find out how to read this from Log Capabilities for AddToStore
        private readonly int _maxDataNodes = 5000;
        private readonly int _maxDataPoints = 10000;

        private readonly string[] _illeagalColumnIdentifiers = new string[] { "'", "\"", "<", ">", "/", "\\", "&", "," };

        /// <summary>
        /// Initializes a new instance of the <see cref="Log141Validator" /> class.
        /// </summary>
        /// <param name="logDataAdapter">The log data adapter.</param>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        [ImportingConstructor]
        public Log141Validator(IWitsmlDataAdapter<Log> logDataAdapter, IWitsmlDataAdapter<Wellbore> wellboreDataAdapter, IWitsmlDataAdapter<Well> wellDataAdapter)
        {
            _logDataAdapter = logDataAdapter;
            _wellboreDataAdapter = wellboreDataAdapter;
            _wellDataAdapter = wellDataAdapter;
        }


        [ImportMany]
        public IEnumerable<IWitsml141Configuration> Providers { get; set; }

        /// <summary>
        /// Validates the data object while executing AddToStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForInsert()
        {
            var channelCount = DataObject.LogCurveInfo != null ? DataObject.LogCurveInfo.Count : 0;

            // Validate parent uid property
            if (string.IsNullOrWhiteSpace(DataObject.UidWell))
            {
                yield return new ValidationResult(ErrorCodes.MissingParentUid.ToString(), new[] { "UidWell" });
            }
            // Validate parent uid property
            else if (string.IsNullOrWhiteSpace(DataObject.UidWellbore))
            {
                yield return new ValidationResult(ErrorCodes.MissingParentUid.ToString(), new[] { "UidWellbore" });
            }

            // Validate parent exists
            else if (!_wellDataAdapter.Exists(new DataObjectId(DataObject.UidWell, DataObject.NameWell)))
            {
                yield return new ValidationResult(ErrorCodes.MissingParentDataObject.ToString(), new[] { "UidWell" });
            }
            // Validate parent exists
            else if (!_wellboreDataAdapter.Exists(new WellObjectId(DataObject.UidWellbore, DataObject.UidWell, DataObject.NameWellbore)))
            {
                yield return new ValidationResult(ErrorCodes.MissingParentDataObject.ToString(), new[] { "UidWellbore" });
            }

            // Validate UID does not exist
            else if (_logDataAdapter.Exists(DataObject.GetObjectId()))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectUidAlreadyExists.ToString(), new[] { "Uid" });
            }

            // Validate that column-identifiers in LogCurveInfo are unique
            else if (DataObject.LogCurveInfo != null
                && DataObject.LogCurveInfo.GroupBy(lci => lci.Mnemonic.Value)
                .Select(group => new { Menmonic = group.Key, Count = group.Count() })
                .Any(g => g.Count > 1))
            {
                yield return new ValidationResult(ErrorCodes.DuplicateColumnIdentifiers.ToString(), new[] { "LogCurveInfo", "Mnemonic" });
            }

            // Validate that column-identifiers in all LogData MnemonicLists are unique.
            else if (DataObject.LogData != null
                && DataObject.LogData.Count > 0
                && !DataObject.LogData.All(ld => ld.MnemonicList.Split(',').Count() == ld.MnemonicList.Split(',').Distinct().Count()))
            {
                yield return new ValidationResult(ErrorCodes.DuplicateColumnIdentifiers.ToString(), new[] { "LogData", "MnemonicList" });
            }

            // Validate that IndexCurve exists in LogCurveInfo
            else if (!string.IsNullOrEmpty(DataObject.IndexCurve)
                && DataObject.LogCurveInfo != null
                && !DataObject.LogCurveInfo.Any(lci => lci.Mnemonic != null && lci.Mnemonic.Value == DataObject.IndexCurve))
            {
                yield return new ValidationResult(ErrorCodes.IndexCurveNotFound.ToString(), new[] { "IndexCurve" });
            }

            // Validate that Index Curve exists in all LogData mnemonicLists
            else if (!string.IsNullOrEmpty(DataObject.IndexCurve)
                && DataObject.LogData != null
                && DataObject.LogData.Count > 0
                && !DataObject.LogData.All(ld => !string.IsNullOrEmpty(ld.MnemonicList) && ld.MnemonicList.Split(',').Any(mnemonic => mnemonic == DataObject.IndexCurve)))
            {
                yield return new ValidationResult(ErrorCodes.IndexCurveNotFound.ToString(), new[] { "IndexCurve" });
            }

            // Validate if MaxDataNodes has been exceeded
            else if (DataObject.LogData != null && DataObject.LogData.SelectMany(ld => ld.Data).Count() > _maxDataNodes)
            {
                yield return new ValidationResult(ErrorCodes.MaxDataExceeded.ToString(), new[] { "LogData", "Data" });
            }

            // Validate if MaxDataPoints has been exceeded
            else if (DataObject.LogData != null 
                && DataObject.LogData.Count > 0 
                && DataObject.LogData.First().Data != null 
                && DataObject.LogData.First().Data.Count > 0 
                && (DataObject.LogData.SelectMany(ld => ld.Data).Count() * DataObject.LogData.First().Data[0].Split(',').Count()) > _maxDataPoints)
            {
                yield return new ValidationResult(ErrorCodes.MaxDataExceeded.ToString(), new[] { "LogData", "Data" });
            }

            // Validate Index Mnemonic is first in LogCurveInfo list
            else if (!string.IsNullOrEmpty(DataObject.IndexCurve) && (DataObject.LogCurveInfo == null || DataObject.LogCurveInfo.Count == 0 || DataObject.LogCurveInfo[0].Mnemonic.Value != DataObject.IndexCurve))
            {
                yield return new ValidationResult(ErrorCodes.IndexNotFirstInDataColumnList.ToString(), new[] { "IndexCurve" });
            }

            // Validate structural-range indices for consistent index types
            else if ((DataObject.StartIndex != null || DataObject.EndIndex != null) && (DataObject.StartDateTimeIndex != null || DataObject.EndDateTimeIndex != null))
            {
                yield return new ValidationResult(ErrorCodes.MixedStructuralRangeIndices.ToString(), new[] { "StartIndex", "EndIndex", "StartDateTimeIndex", "EndDateTimeIndex" });
            }

            // Validate for a bad column identifier in LogCurveInfo Mnemonics
            else if (_illeagalColumnIdentifiers.Any(s => DataObject.LogCurveInfo.Any(m => m.Mnemonic.Value.Contains(s))))
            {
                yield return new ValidationResult(ErrorCodes.BadColumnIdentifier.ToString(), new[] { "LogCurveInfo.Mnemonic" });
            }

            // Validate for a bad column identifier in LogData MnemonicList
            //... If the MnemonicList has more channels than the LogCurveInfo interpret as having a comma within a mnemonic which is a bad column identifier
            else if (DataObject.LogData.Select(ld => ld.MnemonicList.Split(',')).Any(a => a.Count() > channelCount))
            {
                yield return new ValidationResult(ErrorCodes.BadColumnIdentifier.ToString(), new[] { "LogData.MnemonicList" });
            }

            // Inspect each mnemonic, in each mnemonicList, in each LogData for an illeagal column identifier.
            else if (DataObject.LogData.Select(ld => ld.MnemonicList.Split(','))
                .Any(mnemArrary => mnemArrary
                    .Any(mnemonic => _illeagalColumnIdentifiers
                        .Any(badChar => mnemonic.Contains(badChar)))))
            {
                yield return new ValidationResult(ErrorCodes.BadColumnIdentifier.ToString(), new[] { "LogData.MnemonicList" });
            }
        }
    }
}
