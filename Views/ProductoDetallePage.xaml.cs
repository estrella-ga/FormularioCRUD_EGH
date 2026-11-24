using System.Globalization;
using CatalogoMAUI_EGH.Models;
using CatalogoMAUI_EGH.Services;

namespace CatalogoMAUI_EGH.Views;

public partial class ProductoDetallePage : ContentPage, IQueryAttributable
{
    private int id;
    private string fotoActual = "mochila.png";
    private string? rutaTemporalNueva;

    public ProductoDetallePage()
    {
        InitializeComponent();
        Title = "Agregar producto";
        Foto.ItemsSource = new List<string> { "mochila.png", "cuaderno.png", "termo.png" };
        Foto.SelectedIndex = 0;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("producto", out var dato) || dato is not Producto p)
            return;

        id = p.Id;
        fotoActual = p.Foto;
        rutaTemporalNueva = null;

        Title = "Editar producto";
        Nombre.Text = p.Nombre;
        Descripcion.Text = p.Descripcion;
        Detalle.Text = p.Detalle;
        Precio.Text = p.Precio.ToString("0.00", CultureInfo.CurrentCulture);

        if (Foto.ItemsSource is List<string> incluidas && incluidas.Contains(p.Foto))
            Foto.SelectedItem = p.Foto;
        else
            Foto.SelectedItem = null;

        VistaFoto.Source = p.FotoVisible;
        Eliminar.IsVisible = true;
    }

    private void AlCambiarFoto(object? sender, EventArgs e)
    {
        if (Foto.SelectedItem is string archivo)
        {
            rutaTemporalNueva = null;
            fotoActual = archivo;
            VistaFoto.Source = archivo;
        }
    }

    private async void AlElegirFoto(object? sender, EventArgs e)
    {
        try
        {
            var temp = await FotosProducto.ElegirAsync();
            if (temp is null) return;

            rutaTemporalNueva = temp;
            Foto.SelectedItem = null;
            VistaFoto.Source = temp;
        }
        catch (Exception ex)
        {
            await DisplayAlert("No se pudo cargar la foto", ex.Message, "Aceptar");
        }
    }

    private async void AlGuardar(object? sender, EventArgs e)
    {
        if (!Guardar.IsEnabled) return;

        var textoPrecio = Precio.Text?.Trim().Replace(',', '.');
        if (string.IsNullOrWhiteSpace(Nombre.Text) ||
            string.IsNullOrWhiteSpace(Descripcion.Text) ||
            string.IsNullOrWhiteSpace(Detalle.Text) ||
            !decimal.TryParse(textoPrecio, NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var importe) ||
            importe < 0 || importe > 99999999.99m ||
            decimal.Round(importe, 2) != importe ||
            (rutaTemporalNueva is null && string.IsNullOrWhiteSpace(fotoActual)))
        {
            await DisplayAlert("Revisa los datos",
                "Completa los campos y escribe un precio válido de hasta dos decimales.", "Aceptar");
            return;
        }

        string fotoAnterior = fotoActual;
        string? fotoNuevaGuardada = null;

        await EjecutarAsync(async () =>
        {
            string nombreFotoFinal = fotoAnterior;

            if (rutaTemporalNueva is not null)
            {
                fotoNuevaGuardada = await FotosProducto.GuardarAsync(rutaTemporalNueva);
                nombreFotoFinal = fotoNuevaGuardada;
            }

            var producto = new Producto
            {
                Id = id,
                Nombre = Nombre.Text.Trim(),
                Descripcion = Descripcion.Text.Trim(),
                Detalle = Detalle.Text.Trim(),
                Precio = importe,
                Foto = nombreFotoFinal
            };

            try
            {
                if (id == 0)
                    await ProductosSqlite.Instancia.Insertar(producto);
                else if (!await ProductosSqlite.Instancia.Actualizar(producto))
                    throw new InvalidOperationException("El producto ya no existe.");

                if (fotoNuevaGuardada is not null)
                    FotosProducto.EliminarSiEsPropia(fotoAnterior);

                return true;
            }
            catch
            {
                if (fotoNuevaGuardada is not null)
                    FotosProducto.EliminarSiEsPropia(fotoNuevaGuardada);
                throw;
            }
        });
    }

    private async void AlEliminar(object? sender, EventArgs e)
    {
        if (!Eliminar.IsEnabled || id == 0) return;

        await EjecutarAsync(async () =>
        {
            if (!await DisplayAlert("Eliminar producto", "¿Deseas eliminar este producto?",
                "Eliminar", "Cancelar"))
                return false;

            if (!await ProductosSqlite.Instancia.Eliminar(id))
                throw new InvalidOperationException("El producto ya no existe.");

            FotosProducto.EliminarSiEsPropia(fotoActual);
            return true;
        });
    }

    private async Task EjecutarAsync(Func<Task<bool>> operacion)
    {
        Guardar.IsEnabled = Eliminar.IsEnabled = ElegirFoto.IsEnabled = false;
        try
        {
            if (await operacion())
                await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("No se completó la operación", ex.Message, "Aceptar");
        }
        finally
        {
            Guardar.IsEnabled = Eliminar.IsEnabled = ElegirFoto.IsEnabled = true;
        }
    }
}