using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Security.Cryptography;

namespace SistemaVotoAPI.Security
{
    public static class PasswordHasher
    {
        /*
         Este método se usa cuando se guarda una contraseña por primera vez.
         Nunca se almacena la contraseña original.
         Se genera un salt aleatorio y se aplica un algoritmo de derivación seguro.
         El resultado se guarda como: salt.hash
        */
        public static string Hash(string password)
        {
            // Genera un salt aleatorio para evitar ataques por tablas rainbow
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            // Genera el hash usando PBKDF2 con HMACSHA256
            var hash = KeyDerivation.Pbkdf2(
                password,
                salt,
                KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 32
            );

            // Se almacenan juntos salt y hash para poder verificar después
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        /*
         Este método se usa en el login.
         Recibe la contraseña escrita por el usuario y el hash almacenado.
         Extrae el salt, vuelve a generar el hash y compara de forma segura.
         Devuelve true si la contraseña es correcta.
        */
        public static bool Verify(string password, string hashedPassword)
        {
            // Separa el salt y el hash almacenados
            var parts = hashedPassword.Split('.');
            if (parts.Length != 2)
                return false;

            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = Convert.FromBase64String(parts[1]);

            // Genera el hash usando el mismo salt y parámetros
            var computedHash = KeyDerivation.Pbkdf2(
                password,
                salt,
                KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 32
            );

            // Comparación segura para evitar ataques por tiempo
            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }
    }
}
