using Neo4jIntegration.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Models
{
    [DbRequireParent]
    public class ValueAsNode<T> : INeo4jNode where T: unmanaged
    {
        [ID]
        public string Id
        {
            get
            {
                Type t = typeof(T);
                if (t == typeof(byte)
                  || t == typeof(sbyte)
                  || t == typeof(short)
                  || t == typeof(ushort)
                  || t == typeof(int)
                  || t == typeof(uint)
                  || t == typeof(long)
                  || t == typeof(ulong)
                  || t == typeof(float)
                  || t == typeof(double)
                  || t == typeof(decimal)
                  || t == typeof(string)
                )
                {
                    return Value.ToString();
                }
                else if (t == typeof(Guid))
                {
                    unsafe
                    {
                        T val = Value;
                        T* valRef = &val;
                        Guid* gref = (Guid*)valRef;
                        Guid g = gref[0];
                        return g.ToString("n");
                    }
                }
                else if (t == typeof(DateTime))
                {
                    unsafe
                    {
                        T val = Value;
                        T* valRef = &val;
                        DateTime* gref = (DateTime*)valRef;
                        DateTime g = gref[0];
                        return g.ToString("o");
                    }
                }
                else if (t == typeof(TimeSpan))
                {
                    unsafe
                    {
                        T val = Value;
                        T* valRef = &val;
                        TimeSpan* gref = (TimeSpan*)valRef;
                        TimeSpan g = gref[0];
                        return g.ToString("o");
                    }
                }
                else
                {
                    unsafe
                    {
                        T val = Value;
                        Span<byte> raw = new Span<byte>(&val, sizeof(T));
                        string outp = new string((char*)&val).Substring(0, raw.Length / sizeof(char));
                        return outp;
                    }
                }
            }
            set
            {
            }
        }
        public bool IsActive { get; set; }
        public T Value { get; set; }

        public static implicit operator T(ValueAsNode<T> rhs)
        {
            return rhs.Value;
        }
        public static implicit operator ValueAsNode<T>(T rhs)
        {
            return new ValueAsNode<T>()
            {
                Value = rhs,
                IsActive = true
            };
        }
    }
}
