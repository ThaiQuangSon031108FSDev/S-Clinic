// Quick script to generate BCrypt hash and update DB
// Run: dotnet script fix-password.csx  OR  dotnet run fix-password.csx
using System;
using System.Data.SqlClient;
using BCrypt.Net;

var newHash = BCrypt.Net.BCrypt.HashPassword("Sclinic@123", workFactor: 11);
Console.WriteLine("New Hash: " + newHash);
