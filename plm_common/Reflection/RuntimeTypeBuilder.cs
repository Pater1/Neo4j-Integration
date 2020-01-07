using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
        using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;

namespace plm_common.Reflection
{
    public static class RuntimeTypeBuilder
    {
        public class FieldDescriptor
        {
            public FieldDescriptor(string fieldName, Type fieldType)
            {
                FieldName = fieldName;
                FieldType = fieldType;
            }
            public string FieldName { get; }
            public Type FieldType { get; }
        }

        public static class MyTypeBuilder
        {
            //public static object CreateNewObject()
            //{
            //    var myTypeInfo = CompileResultTypeInfo();
            //    var myType = myTypeInfo.AsType();
            //    var myObject = Activator.CreateInstance(myType);

            //    return myObject;
            //}

            public static TypeInfo CompileResultTypeInfo(List<FieldDescriptor> fieldList)
            {
                TypeBuilder tb = GetTypeBuilder();
                //ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
                ConstructorBuilder constructor = tb.DefineConstructor(
                    MethodAttributes.Public,
                    CallingConventions.HasThis,
                    fieldList.Select(x => x.FieldType).ToArray()
                );
                ConstructorBuilder constructor2 = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
                ILGenerator cotrIL = constructor.GetILGenerator();


                for(int i = 0; i < fieldList.Count; i++)
                {
                    //var paramInfo = constructor.DefineParameter(i, ParameterAttributes.None, "__" + fieldList[i].FieldName);

                    PropertyBuilder propB = CreateProperty(tb, fieldList[i].FieldName, fieldList[i].FieldType);

                    //cotrIL.Emit(OpCodes.Ldarg, i);

                    //cotrIL.Emit(OpCodes.Call, propB.GetSetMethod(true));//set the value of the coresponding property
                }
                cotrIL.Emit(OpCodes.Ret);

                TypeInfo objectTypeInfo = tb.CreateTypeInfo();
                return objectTypeInfo;
            }

            private static TypeBuilder GetTypeBuilder()
            {
                var typeSignature = "RuntimeMapper" + Guid.NewGuid().ToString("N");
                var an = new AssemblyName(typeSignature);
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
                TypeBuilder tb = moduleBuilder.DefineType(typeSignature,
                        TypeAttributes.Public |
                        TypeAttributes.Class |
                        TypeAttributes.AutoClass |
                        TypeAttributes.AnsiClass |
                        TypeAttributes.BeforeFieldInit |
                        TypeAttributes.AutoLayout,
                        null);
                return tb;
            }

            private static PropertyBuilder CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
            {
                FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

                PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
                MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
                ILGenerator getIl = getPropMthdBldr.GetILGenerator();

                getIl.Emit(OpCodes.Ldarg_0);
                getIl.Emit(OpCodes.Ldfld, fieldBuilder);
                getIl.Emit(OpCodes.Ret);

                MethodBuilder setPropMthdBldr =
                    tb.DefineMethod("set_" + propertyName,
                      MethodAttributes.Public |
                      MethodAttributes.SpecialName |
                      MethodAttributes.HideBySig,
                      null, new[] { propertyType });

                ILGenerator setIl = setPropMthdBldr.GetILGenerator();
                Label modifyProperty = setIl.DefineLabel();
                Label exitSet = setIl.DefineLabel();

                setIl.MarkLabel(modifyProperty);
                setIl.Emit(OpCodes.Ldarg_0);
                setIl.Emit(OpCodes.Ldarg_1);
                setIl.Emit(OpCodes.Stfld, fieldBuilder);

                setIl.Emit(OpCodes.Nop);
                setIl.MarkLabel(exitSet);
                setIl.Emit(OpCodes.Ret);

                propertyBuilder.SetGetMethod(getPropMthdBldr);
                propertyBuilder.SetSetMethod(setPropMthdBldr);

                return propertyBuilder;
            }
        }
    }
}