using Newtonsoft.Json;
using plm_common.Attributes;
using plm_common.DB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace plm_common.Models.Versioning
{
    public class Versionable<T> : INeo4jNode, IEnumerable<VersionableItteration<T>>, NoDBCollection /*,IEnumerable<T>*/// ,ICustomDBSchema<Versionable<T>>
    {
        [ID(ID.IDType.String, ID.CollisionResolutionStrategy.Rand_Base62_10)]
        public string Id { get; private set; }
        public bool IsActive { get; set; } = true;

        [DBIgnore]
        public VersionableItteration<T> Start => ((ICollection<VersionableItteration<T>>)Versions).FirstOrDefault();
        [DBIgnore]
        public VersionableItteration<T> Last => ((ICollection<VersionableItteration<T>>)Versions).LastOrDefault();
        [DBIgnore]
        public IDictionary<AcceptanceState, T> Latest => ((IEnumerable<VersionableItteration<T>>)this)
            .GroupBy(x => x.AcceptanceState).ToDictionary(x => x.Key, x => x.LastOrDefault().Value);

        [ReferenceThroughRelationship((string)null)]
        public ReferenceCollection<VersionableItteration<T>> Versions { get; private set; }
            = new ReferenceCollection<VersionableItteration<T>>((a, b) => a.ItteratedTime.CompareTo(b.ItteratedTime));


        public void Add(T value, AcceptanceState acceptanceState)
        {
            Add(new VersionableItteration<T>(value, acceptanceState));
        }
        public void Add(VersionableItteration<T> value)
        {
            //var latest = Last;
            //value.Previous = latest;
            //latest.Next = value;

            Versions.Add(value);
        }

        public IEnumerator<VersionableItteration<T>> GetEnumerator()
        {
            return Versions.GetEnumerator();
            //VersionableItteration<T> current = Start;
            //while(current != null)
            //{
            //    yield return current;
            //    current = current.Next;
            //}
        }

        //IEnumerator<T> IEnumerable<T>.GetEnumerator()
        //{
        //    IEnumerator<VersionableItteration<T>> enu = this.GetEnumerator();
        //    while (enu.MoveNext())
        //    {
        //        yield return enu.Current.Value;
        //    }
        //}

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        //public SaveQuearyParams<Versionable<T>> SaveValue(SaveQuearyParams<Versionable<T>> qParams)
        //{
        //    if(Start == null)
        //    {
        //        return qParams;
        //    }

        //    var undoParams = qParams;
        //    qParams = qParams.Save();

        //    VersionableItteration<T> pre = null;
        //    SaveQuearyParams<Versionable<T>> preParam = default;

        //    foreach (VersionableItteration<T> v in this)
        //    {
        //        SaveQuearyParams<Versionable<T>> sParams = qParams.ChainSaveNode(v);
        //        if (sParams.success)
        //        {
        //            if (pre == null)
        //            {
        //                sParams = DBOps<Versionable<T>>.Writes<Versionable<T>>.WriteRelationship(sParams, qParams.objName, sParams.objName, "START", RelationshipDirection.Forward);
        //            }
        //            else
        //            {
        //                var a = DBOps<Versionable<T>>.Writes<Versionable<T>>.WriteRelationship(sParams, preParam.objName, sParams.objName, "NEXT", RelationshipDirection.Forward);
        //                sParams = DBOps<Versionable<T>>.Writes<Versionable<T>>.WriteRelationship(a, preParam.objName, sParams.objName, "PREVIOUS", RelationshipDirection.Reverse);
        //            }

        //            pre = v;
        //            sParams = DBOps<Versionable<T>>.Writes<Versionable<T>>.WriteRelationship(sParams, qParams.objName, sParams.objName, "VERSION", RelationshipDirection.Forward);
        //            preParam = sParams;
        //        }
        //    }

        //    if (pre != null)
        //    {
        //        preParam = DBOps<Versionable<T>>.Writes<Versionable<T>>.WriteRelationship(preParam, qParams.objName, preParam.objName, "END", RelationshipDirection.Forward);
        //        qParams.queary = preParam.queary;
        //        qParams.success = true;
        //        return qParams;
        //    }
        //    else
        //    {
        //        undoParams.success = false;
        //        return undoParams;
        //    }
        //}

        //public ReadQueryParams<Versionable<T>> ReadValue(ReadQueryParams<Versionable<T>> rParams)
        //{
        //    throw new NotImplementedException();
        //}

        private static readonly JsonSerializerSettings inlineSerializer = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            FloatFormatHandling = FloatFormatHandling.DefaultValue,
            NullValueHandling = NullValueHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Auto,
            PreserveReferencesHandling = PreserveReferencesHandling.All
        };

        public static implicit operator VersionableItteration<T>(Versionable<T> vit)
        {
            return vit.Last;
        }
    }
}
