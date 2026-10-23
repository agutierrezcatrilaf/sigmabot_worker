using SigmabotSync.Application.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SigmabotSync.Application.Extraction
{
    /// <summary>Agrupa fallos de una etapa para el log de usuario. El detalle técnico queda en depuración.</summary>
    public sealed class ExtraccionErroresUsuario
    {
        private readonly object _lock = new object();
        private readonly List<Tuple<string, string>> _items = new List<Tuple<string, string>>();

        public void Agregar(string titulo, string item, string detalle)
        {
            if (!string.IsNullOrWhiteSpace(detalle))
                Utilities.Wlog(detalle, 1);
            lock (_lock)
                _items.Add(Tuple.Create(titulo ?? "No se pudo completar la etapa", item));
        }

        public void Escribir()
        {
            List<Tuple<string, string>> copia;
            lock (_lock)
                copia = _items.ToList();
            if (copia.Count == 0)
                return;

            Utilities.Wlog("", 0);
            foreach (var grupo in copia
                .GroupBy(error => error.Item1, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(grupo => grupo.Count()))
            {
                var items = grupo.Select(error => error.Item2).Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
                if (items.Count == 0)
                {
                    Utilities.Wlog(grupo.Key + ".", 0);
                    continue;
                }

                Utilities.Wlog(grupo.Key + ": " + items.Count + ".", 0);
                int visibles = Math.Min(items.Count, 30);
                for (int i = 0; i < visibles; i++)
                    Utilities.Wlog("  - " + items[i], 0);
                if (items.Count > visibles)
                    Utilities.Wlog("  - y " + (items.Count - visibles) + " más", 0);
            }
        }
    }
}
