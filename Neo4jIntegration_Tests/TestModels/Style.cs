using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Neo4jIntegration.Attributes;
using Neo4jIntegration.Models;
using Neo4jIntegration.Models.Versioning;

namespace Neo4jIntegration_Tests.TestModels
{
    public class Style : INeo4jNode, ITemplatable<Style>
    {
        [ID(IDAttribute.CollisionResolutionStrategy.Rand_Base64_10)]
        public string Id { get; set; }
        public bool IsActive { get; set; } = true;

        public string Name { get; set; }


        [DbName("TEMPLATE")]
        public Style Template { get; set; }

        [DbName("TEMPLATE_Versions")]
        public Versionable<Style> TemplateVers { get; set; }

        [DbName("WAISTLINE_VERISONS")]
        public Versionable<ICollection<float>> WaistLine { get; private set; } = new Versionable<ICollection<float>>();

        [DbName("COLORS_VERSIONS")]
        public Versionable<ICollection<Color>> Colors { get; private set; } = new Versionable<ICollection<Color>>();

        [DbName("CATEGORY_VERSIONS")]
        public Versionable<Category> Category { get; private set; } = new Versionable<Category>();



        //[ReferenceThroughRelationship(typeof(VersionableRelationship<ICollection<Fabric>>))]
        //public Versionable<ICollection<Fabric>> Fabrics { get; private set; } = new Versionable<ICollection<Fabric>>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<ICollection<float>>))]
        //public Versionable<ICollection<float>> Sizes { get; private set; } = new Versionable<ICollection<float>>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<ICollection<float>>))]
        //public Versionable<ICollection<float>> Inseam { get; private set; } = new Versionable<ICollection<float>>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<bool>))]
        //public Versionable<bool> InElastic { get; private set; } = new Versionable<bool>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<string>))]
        //public Versionable<string> Progress { get; private set; } = new Versionable<string>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<string>))]
        //public Versionable<string> StyleNumber { get; private set; } = new Versionable<string>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<string>))]
        //public Versionable<string> StyleName { get; private set; } = new Versionable<string>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<Factory>))]
        //public Versionable<Factory> Factory { get; private set; } = new Versionable<Factory>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<long>))]
        //public Versionable<long> CatalogOrder { get; private set; } = new Versionable<long>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<string>))]
        //public Versionable<string> Season { get; private set; } = new Versionable<string>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<string>))]
        //public Versionable<string> CountryOfOrigin { get; private set; } = new Versionable<string>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<Label>))]
        //public Versionable<Label> Label { get; private set; } = new Versionable<Label>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<Division>))]
        //public Versionable<Division> Division { get; private set; } = new Versionable<Division>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<string>))]
        //public Versionable<string> ProductLine { get; private set; } = new Versionable<string>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<string>))]
        //public Versionable<string> ProductType { get; private set; } = new Versionable<string>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<Fit>))]
        //public Versionable<Fit> Fit { get; private set; } = new Versionable<Fit>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<string>))]
        //public Versionable<string> BottomOpening { get; private set; } = new Versionable<string>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<string>))]
        //public Versionable<string> ExtendedSizing { get; private set; } = new Versionable<string>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<string>))]
        //public Versionable<string> SetNumber { get; private set; } = new Versionable<string>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<string>))]
        //public Versionable<string> LayoutSize { get; private set; } = new Versionable<string>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<string>))]
        //public Versionable<string> packaging { get; private set; } = new Versionable<string>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<long>))]
        //public Versionable<long> GarmentWeightGrams { get; private set; } = new Versionable<long>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<float>))]
        //public Versionable<float> GarmentWeightOz { get; private set; } = new Versionable<float>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<string>))]
        //public Versionable<string> HangtagWebFeatures { get; private set; } = new Versionable<string>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<DateTime>))]
        //public Versionable<DateTime> DeliveryDate { get; private set; } = new Versionable<DateTime>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<float>))]
        //public Versionable<float> SQMUsedEstimate { get; private set; } = new Versionable<float>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<float>))]
        //public Versionable<float> YardUsedEstimate { get; private set; } = new Versionable<float>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<bool>))]
        //public Versionable<bool> IsNew { get; private set; } = new Versionable<bool>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<bool>))]
        //public Versionable<bool> IsUpdated { get; private set; } = new Versionable<bool>();

        //[ReferenceThroughRelationship(typeof(VersionableRelationship<string>))]
        //public Versionable<string> ProductName { get; set; } = new Versionable<string>();


        //public Versionable<string> Designer { get; private set; } //relationship to user
        //public Versionable<string> Developer { get; private set; }


        //public Versionable<string> SizeMatrix { get; private set; }//make a class
        //public Versionable<string> FeatureBoxIcons { get; private set; }//json
        //public Versionable<string> Activities { get; private set; }//json
        //public Versionable<string> Benefits { get; private set; }//json
        //public Versionable<string> Sustains { get; private set; }//json
        //public Versionable<string> Features { get; private set; }//json
    }

}

