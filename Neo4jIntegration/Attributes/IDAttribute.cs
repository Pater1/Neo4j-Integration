using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Neo4jIntegration.Attributes
{
    public class IDAttribute : DbNameAttribute, INeo4jAttribute
    {
        CollisionResolutionStrategy collisionResolutionStrategy;
        public IDAttribute(CollisionResolutionStrategy collisionResolutionStrategy = CollisionResolutionStrategy.Rand_Base64_10)
            : base("Id")
        {
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
            Rand_Base64_10,
            //Rand_Base32_10,
        }

        private const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        private const string alphabet32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        private static readonly RNGCryptoServiceProvider cryptoRand = new RNGCryptoServiceProvider();

        public string GenerateId()
        {
            Random rand = new Random();
            switch (collisionResolutionStrategy)
            {
                case CollisionResolutionStrategy.Long_Random:
                    return ((((long)rand.Next()) << 32) | ((long)rand.Next())).ToString();
                case CollisionResolutionStrategy.Long_DateTime:
                    return DateTime.UtcNow.Ticks.ToString();
                case CollisionResolutionStrategy.Rand_Base64_10:
                    Span<byte> rndBytes = stackalloc byte[10];
                    Span<char> rndChar = stackalloc char[10];

                    cryptoRand.GetBytes(rndBytes);

                    for (int i = 0; i < rndBytes.Length; i++)
                    {
                        rndChar[i] = alphabet[rndBytes[i] % alphabet.Length];
                    }

                    return new string(rndChar);
                case CollisionResolutionStrategy.Guid_New:
                    return Guid.NewGuid().ToString("n");
                case CollisionResolutionStrategy.ErrorOut:
                    throw new FormatException($"Null Id found without resolution strategy!");
                default:
                    throw new FormatException($"");
            }
        }
    }
}
