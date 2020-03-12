using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Neo4jIntegration.Attributes
{
    public class ID : DbNameAttribute, INeo4jAttribute
    {
        IDType idType;
        CollisionResolutionStrategy collisionResolutionStrategy;
        public ID(IDType idType, CollisionResolutionStrategy collisionResolutionStrategy = CollisionResolutionStrategy.Rand_Base62_10)
            : base("Id")
        {
            this.idType = idType;
            this.collisionResolutionStrategy = collisionResolutionStrategy;
        }
        public enum CollisionResolutionStrategy
        {
            ErrorOut,
            //Long_NextAvaliable,
            Long_Random,
            Long_DateTime,
            String_AppendGuid,
            String_ReplaceWithGuid,
            Guid_New,
            Rand_Base62_10,
        }
        public enum IDType
        {
            Long,
            String,
            Guid
        }
        public bool OnWrite(DependencyInjector depInj)
        {
            bool wrote = false;
            object o = depInj.Get("value");
            switch (idType)
            {
                case IDType.Long:
                    if (o.GetType() != typeof(long)) TypeError("long");
                    long l = (long)o;
                    if(l == default(long))
                    {
                        if(collisionResolutionStrategy == CollisionResolutionStrategy.ErrorOut)
                        {
                            ValueError(l.ToString());
                        }
                        else
                        {
                            l = Resolve(l, depInj);
                            wrote = true;
                        }
                    }
                    o = l;
                    break;
                case IDType.String:
                    if (o != null && o.GetType() != typeof(string)) TypeError("String");
                    string s = (string)o;
                    if (string.IsNullOrWhiteSpace(s))
                    {

                        if (collisionResolutionStrategy == CollisionResolutionStrategy.ErrorOut)
                        {
                            ValueError(s.ToString());
                        }
                        else
                        {
                            s = Resolve(s, depInj);
                            wrote = true;
                        }
                    }
                    o = s;
                    break;
                case IDType.Guid:
                    if (o.GetType() != typeof(Guid)) TypeError("Guid");
                    Guid g = (Guid)o;
                    if (g == default(Guid))
                    {

                        if (collisionResolutionStrategy == CollisionResolutionStrategy.ErrorOut)
                        {
                            ValueError(g.ToString());
                        }
                        else
                        {
                            g = Resolve(g, depInj);
                            wrote = true;
                        }
                    }
                    o = g;
                    break;
            }
            depInj.Set("value", o);
            return wrote;
        }


        private long Resolve(long l, DependencyInjector depInj)
        {
            Random rand = new Random();
            switch (collisionResolutionStrategy)
            {
                case CollisionResolutionStrategy.Long_Random:
                    return (((long)rand.Next()) << 32) | ((long)rand.Next());
                case CollisionResolutionStrategy.Long_DateTime:
                    return DateTime.UtcNow.Ticks;
                default:
                    throw new FormatException($"The ID generation strategy {collisionResolutionStrategy} is not valid for IDs of type {idType}");
            }
        }
        private static readonly IReadOnlyList<char> letters = new char[]
        {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
            'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
        }.ToList().AsReadOnly();
        private static readonly IReadOnlyList<char> chars =
            Enumerable.Range(0, 10).Select(x => x.ToString()[0])
                .Concat(letters)
                .Concat(letters.Select(x => char.ToUpper(x)))
                .ToList().AsReadOnly();
        private static readonly RNGCryptoServiceProvider cryptoRand = new RNGCryptoServiceProvider();
        private string Resolve(string s, DependencyInjector depInj)
        {
            switch (collisionResolutionStrategy)
            {
                case CollisionResolutionStrategy.String_AppendGuid:
                    s = s + Guid.NewGuid().ToString();
                    break;
                case CollisionResolutionStrategy.String_ReplaceWithGuid:
                    s = Guid.NewGuid().ToString();
                    break;
                case CollisionResolutionStrategy.Rand_Base62_10:
                    Span<byte> rndBytes = stackalloc byte[10];
                    Span<char> rndChar = stackalloc char[10];

                    cryptoRand.GetBytes(rndBytes);

                    for(int i = 0; i < rndBytes.Length; i++)
                    {
                        rndChar[i] = chars[rndBytes[i] % chars.Count];
                    }

                    s = new string(rndChar);
                    break;
                default:
                    throw new FormatException($"The ID generation strategy {collisionResolutionStrategy} is not valid for IDs of type {idType}");
            }
            return s;
            //return "\"" + s + "\"";
        }
        private Guid Resolve(Guid g, DependencyInjector depInj)
        {
            switch (collisionResolutionStrategy)
            {
                case CollisionResolutionStrategy.Guid_New:
                    return Guid.NewGuid();
                default:
                    throw new FormatException($"The ID generation strategy {collisionResolutionStrategy} is not valid for IDs of type {idType}");
            }
        }

        private void ValueError(string idValue)
        {
            throw new FormatException($"The value {idValue} is not a valid ID for this object");
        }
        private void TypeError(string typeName)
        {
            throw new FormatException($"This ID attribute if configured for IDs of type {idType.ToString()}, a value of type {typeName} was provided");
        }

        public void OnRead(DependencyInjector depInj)
        {
            string o = depInj.Get("value").ToString();
            depInj.Set("value", new string(o.Skip(1).Reverse().Skip(1).Reverse().ToArray()));
        }
    }
}
