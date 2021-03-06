﻿using System;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.Serialization.Eve
{
    /// <summary>
    /// Represents a serializable version of a character's info. Used for querying CCP and settings.
    /// </summary>
    public sealed class SerializableAPICharacterInfo
    {
        private readonly Collection<SerializableEmploymentHistoryListItem> m_employmentHistory;

        public SerializableAPICharacterInfo()
        {
            m_employmentHistory = new Collection<SerializableEmploymentHistoryListItem>();
        }

        [XmlElement("shipName")]
        public string ShipNameXml
        {
            get { return ShipName; }
            set { ShipName = value?.HtmlDecode() ?? String.Empty; }
        }

        [XmlElement("shipTypeName")]
        public string ShipTypeName { get; set; }

        [XmlElement("lastKnownLocation")]
        public string LastKnownLocation { get; set; }

        [XmlElement("securityStatus")]
        public double SecurityStatus { get; set; }

        [XmlArray("employmentHistory")]
        [XmlArrayItem("record")]
        public Collection<SerializableEmploymentHistoryListItem> EmploymentHistory => m_employmentHistory;

        [XmlIgnore]
        public string ShipName { get; set; }
    }
}