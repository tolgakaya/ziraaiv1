#r "nuget: Npgsql, 8.0.4"

using System;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";
using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

var query = "SELECT column_name, data_type, is_nullable FROM information_schema.columns WHERE table_name = 'DeepLinks' ORDER BY ordinal_position;";
using var command = new NpgsqlCommand(query, connection);
using var reader = await command.ExecuteReaderAsync();

Console.WriteLine("Column\t\t\tType\t\tNullable");
while (await reader.ReadAsync())
{
    Console.WriteLine($"{reader.GetString(0),-20}\t{reader.GetString(1),-15}\t{reader.GetString(2)}");
}
