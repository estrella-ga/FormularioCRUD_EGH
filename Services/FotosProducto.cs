using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogoMAUI_EGH.Services
{
    internal class FotosProducto
    {
        private const long TamanoMaximoBytes = 15L * 1024 * 1024; // 15 MB
        private static readonly string CarpetaFotos =
            Path.Combine(FileSystem.AppDataDirectory, "fotos_productos");

        public static async Task<string?> ElegirAsync()
        {
            var resultado = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Selecciona una imagen del producto"
            });

            if (resultado is null)
                return null;

            string ext = Path.GetExtension(resultado.FileName).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                throw new InvalidOperationException("Solo se permiten imágenes JPG, JPEG o PNG.");

            await using var origen = await resultado.OpenReadAsync();
            if (origen.Length > TamanoMaximoBytes)
                throw new InvalidOperationException("La imagen supera el límite de 15 MB.");

            string nombreTemp = $"preview_{Guid.NewGuid():N}{ext}";
            string rutaTemp = Path.Combine(FileSystem.CacheDirectory, nombreTemp);

            await using var destino = File.Create(rutaTemp);
            await origen.CopyToAsync(destino);

            return rutaTemp;
        }

        public static async Task<string> GuardarAsync(string rutaTemporal)
        {
            Directory.CreateDirectory(CarpetaFotos);

            string ext = Path.GetExtension(rutaTemporal).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

            string nombreFinal = $"producto_{Guid.NewGuid():N}{ext}";
            string rutaFinal = Path.Combine(CarpetaFotos, nombreFinal);

            await using var origen = File.OpenRead(rutaTemporal);
            await using var destino = File.Create(rutaFinal);
            await origen.CopyToAsync(destino);

            return nombreFinal;
        }

        public static void EliminarSiEsPropia(string? nombreArchivo)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivo) ||
                !nombreArchivo.StartsWith("producto_", StringComparison.OrdinalIgnoreCase))
                return;

            string ruta = Path.Combine(CarpetaFotos, nombreArchivo);
            if (File.Exists(ruta))
                File.Delete(ruta);
        }

        public static string Resolver(string? nombreArchivo)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivo))
                return string.Empty;

            if (nombreArchivo.StartsWith("producto_", StringComparison.OrdinalIgnoreCase))
                return Path.Combine(CarpetaFotos, nombreArchivo);

            return nombreArchivo;
        }
    }
}
