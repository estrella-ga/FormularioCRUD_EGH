using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogoMAUI_EGH.Models;
using SQLite;

namespace CatalogoMAUI_EGH.Services
{
    public sealed class ProductosSqlite
    {
        public static ProductosSqlite Instancia { get; } = new();
        private readonly Lazy<Task<SQLiteAsyncConnection>> conexion;

        private ProductosSqlite() => conexion = new(AbrirAsync);

        private static async Task<SQLiteAsyncConnection> AbrirAsync()
        {
            //Cada dispositivo guarda su propio archivo en el espacio privado de la app
            string ruta = Path.Combine(FileSystem.AppDataDirectory, "catalogo.db3");
            var db = new SQLiteAsyncConnection(ruta);
            await db.RunInTransactionAsync(c =>
            {
                c.CreateTable<Producto>();
                //La version marca que ya se agregaron los ejemplos. Borrar productos
                //no hace que vuelvan a aparecer al reiniciar
                if (c.ExecuteScalar<int>("PRAGMA user_version") != 0) return;

                c.InsertAll(new[]
                {
                    new Producto {Nombre = "Mochila urbana", Descripcion="Con espacio para laptop",
                        Detalle ="Incluye bolsillo frontal y compartimento acolchado", 
                        Precio = 499m, Foto ="mochila.png"},
                    new Producto {Nombre = "Libreta A5", Descripcion = "80 hojas rayadas.",
                        Detalle = "La pasta dura protege los apuntes.", 
                        Precio = 89.50m, Foto = "cuaderno.png"},
                    new Producto {Nombre = "Termo de acero", Descripcion="Botella reutilizable",
                        Detalle="Capacidad de 500 ml y tapa de rosca",
                        Precio=219m, Foto="termo.png"}
                }, runInTransaction: false);
                c.Execute("PRAGMA user_version = 1");
            });
            return db;
        }

        public async Task<List<Producto>> Consultar() =>
            await (await conexion.Value).Table<Producto>().OrderBy(p => p.Id).ToListAsync();

        public async Task<int> Insertar(Producto producto) =>
            await (await conexion.Value).InsertAsync(producto);
        public async Task<bool> Actualizar(Producto producto) =>
            await (await conexion.Value).UpdateAsync(producto) > 0;
        public async Task<bool> Eliminar(int id) =>
            await (await conexion.Value).DeleteAsync<Producto>(id) > 0;
    }
}
