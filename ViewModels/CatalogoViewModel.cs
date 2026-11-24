using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogoMAUI_EGH.Models;
using System.Collections.ObjectModel;
using CatalogoMAUI_EGH.Models;
using CatalogoMAUI_EGH.Services;

namespace CatalogoMAUI_EGH.ViewModels
{
    public class CatalogoViewModel
    {
        // ObservableCollection notifica a la lista cuando cambian sus elementos.
        public ObservableCollection<Producto> Productos { get; } = new();
        public async Task CargarAsync()
        {
            var productos = await ProductosSqlite.Instancia.Consultar();
            Productos.Clear();
            foreach (var producto in productos)
                Productos.Add(producto);
        }
    }
}
