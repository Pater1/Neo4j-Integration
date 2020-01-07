using Neo4jClient.Cypher;
using Neo4jIntegration.Models;
using Neo4jIntegration.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Neo4jIntegration.DB
{
    public struct SaveQuearyParams<T> where T : class, INeo4jNode, new()
    {
        public ReflectionCache.Property prop;
        public ICypherFluentQuery queary;
        public DependencyInjector depInj;
        public ICollection<object> objIds;
        public IDictionary<string, object> queryParams;
        public ReflectReadDictionary<T> buildFor;
        public string objName;
        public bool isNew;
        public object mutex;
        public long recursionDepth;
        public bool success;

        public object Value { 
            get
            {
                return prop.PullValue(buildFor.backingInstance);
            }
        }

        internal static SaveQuearyParams<T> Build()
        {
            return new SaveQuearyParams<T>();
        }

        public SaveQuearyParams<U> CrosstypedCopy<U>(ReflectReadDictionary<U> newReflectRead, int incrementRecursion = 0) where U : class, INeo4jNode, new()
        {
            SaveQuearyParams<U> copied = new SaveQuearyParams<U>();

            copied.prop = this.prop;
            copied.queary = this.queary;
            copied.depInj = this.depInj;
            copied.objIds = this.objIds;
            copied.queryParams = this.queryParams;
            copied.mutex = this.mutex;
            copied.objName = this.objName;
            copied.isNew = this.isNew;
            copied.success = this.success;

            copied.buildFor = newReflectRead;
            copied.recursionDepth = this.recursionDepth + incrementRecursion;

            return copied;
        }

        internal void Deconstruct(out ICypherFluentQuery queary, out IDictionary<string, object> queryParams)
        {
            queary = this.queary;
            queryParams = this.queryParams;
        }

        public SaveQuearyParams<T> Save()
        {
            SaveQuearyParams<T> ret = this;
            SaveQuearyParams<T> undo = this;

            Type t = typeof(T);
            ret.objName = "obj" + ret.buildFor.StrID.Replace("\"","") + t.QuerySaveName();
            ret.objIds = ret.objIds == null ? new List<object>() : ret.objIds;
            ret.queryParams = ret.queryParams == null ? new Dictionary<string, object>() : ret.queryParams;

            if (ret.objIds.Contains(ret.objName))
            {
                undo.success = false;
                return undo;
            }
            ret.objIds.Add(ret.objName);

            ret.queary = ret.queary.Merge($"({ret.objName}:{t.QuerySaveLabels()} {{ Id: {ret.buildFor.StrID} }})");

            //Parallelize?
            bool wroteAnything = false;
            foreach (var prop in ret.buildFor.propCache.WritePropsList)
            {
                ret.prop = prop;
                ret = ret.SaveValue();

                if (prop.isID)
                {
                    ret.isNew = prop.WrittenTo;
                }
                else if(prop.Name != "IsActive")
                {
                    wroteAnything = true;
                }
            }

            if (ret.recursionDepth == 0)
            {
                ret.queary = ret.queary.WithParams(ret.queryParams);
                ret.queary.ExecuteWithoutResults();
            }

            ret.success = true;
            undo.success = false;

            return wroteAnything? ret: undo;
        }

        private SaveQuearyParams<T> SaveValue()
        {
            return this.prop.customDBSchema.SaveValue(this);
        }

        private static Dictionary<Type, Delegate> writeExpressionCache = new Dictionary<Type, Delegate>();
        public SaveQuearyParams<T> ChainSaveNode(INeo4jNode explicitNode = null)
        {
            Type qt = typeof(SaveQuearyParams<>).MakeGenericType(typeof(T));

            object referenced =
                explicitNode != null ?
                explicitNode :
                this.prop.WriteValidate(this.depInj, this.buildFor.backingInstance);

            if (referenced == null)
            {
                success = false;
                return this;
            }

            Type t = referenced.GetType();
            Type sqt = typeof(SaveQuearyParams<>).MakeGenericType(t);

            Delegate comp;
            if (writeExpressionCache.ContainsKey((t)))
            {
                comp = writeExpressionCache[(t)];
            }
            else
            {
                Type staticRRD = typeof(ReflectReadDictionary);

                var referencedParam = Expression.Parameter(t, "referencedParam");
                MethodInfo build = staticRRD
                    .GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .Single(x => x.GetParameters().Length == 1 && x.Name == nameof(ReflectReadDictionary.Build))
                    .MakeGenericMethod(t);
                Expression buildDict =
                    Expression.Call(
                        null,
                        build,
                        referencedParam
                    );


                var thisParam = Expression.Parameter(typeof(SaveQuearyParams<T>), "thisParam");
                MethodInfo crossCopy = qt
                    .GetMethods()
                    .Single(x => x.GetParameters().Length == 2 && x.Name == nameof(CrosstypedCopy))
                    .MakeGenericMethod(t);
                Expression buildParams =
                    Expression.Call(
                        thisParam,
                        crossCopy,
                        buildDict,
                        Expression.Constant(1)
                    );



                MethodInfo saveTo_custom = t.GetMethod(nameof(SaveValue));
                Expression save;

                if (saveTo_custom != null)
                {
                    save = Expression.Call(
                            referencedParam,
                            saveTo_custom,
                            buildParams
                        );
                }
                else
                {
                    MethodInfo saveTo = sqt
                        .GetMethods()
                        .Single(x => x.GetParameters().Length == 0 && x.Name == "Save");
                    save = Expression.Call(
                        buildParams,
                        saveTo
                    );
                }

                var returnRebuild = Expression.Parameter(typeof(ReflectReadDictionary<T>), "returnRebuild");
                MethodInfo crossRecopy = sqt
                    .GetMethods()
                    .Single(x => x.GetParameters().Length == 2 && x.Name == nameof(CrosstypedCopy))
                    .MakeGenericMethod(typeof(T));

                Expression buildRetParams =
                    Expression.Call(
                        save,
                        crossRecopy,
                        returnRebuild,
                        Expression.Constant(-1)
                    );

                var lambda =
                    Expression.Lambda(
                        buildRetParams,
                        referencedParam,
                        thisParam,
                        returnRebuild
                    );

                comp = lambda.Compile();
            }
            var RQParams = (SaveQuearyParams<T>)comp.DynamicInvoke(referenced, this, this.buildFor);

            //RQParams.success = true;

            return RQParams;
        }
    }
}
