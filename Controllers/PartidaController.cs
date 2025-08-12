using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Globalization;
using System.Text;

namespace Grupo8_Proyecto.Controllers
{
    public class PartidaController : Controller
    {
        private AhorcadoContext db = new AhorcadoContext();

        // GET: Partida/Create
        public ActionResult Create()
        {
            // Verificar que haya usuarios
            if (!db.Usuarios.Any())
            {
                TempData["Error"] = "No hay usuarios registrados. Debe crear al menos un usuario antes de iniciar una partida.";
                return RedirectToAction("Create", "Usuario");
            }

            // Verificar que haya palabras
            if (!db.Palabras.Any())
            {
                TempData["Error"] = "No hay palabras en el diccionario. Debe agregar al menos una palabra antes de iniciar una partida.";
                return RedirectToAction("Create", "Palabras");
            }

            ViewBag.Usuarios = db.Usuarios.Select(u => new SelectListItem
            {
                Value = u.usu_id.ToString(),
                Text = u.usu_id + " - " + u.usu_nombre
            }).ToList();

            return View();
        }

        // POST: Partida/IniciarJuego
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult IniciarJuego(int usuarioId, string nivel)
        {
            try
            {
                // Obtener una palabra aleatoria disponible
                var palabrasDisponibles = db.Palabras.ToList();
                if (!palabrasDisponibles.Any())
                {
                    TempData["Error"] = "No hay palabras disponibles en el diccionario.";
                    return RedirectToAction("Create");
                }

                Random random = new Random();
                var palabraSeleccionada = palabrasDisponibles[random.Next(palabrasDisponibles.Count)];

                // Crear la partida
                var partida = new Partida
                {
                    usu_id = usuarioId,
                    pal_id = palabraSeleccionada.pal_id,
                    par_nivel = nivel,
                    par_resultado = "En Progreso",
                    par_fecha = DateTime.Now
                };

                db.Partidas.Add(partida);
                db.SaveChanges();

                // Redirigir al juego
                return RedirectToAction("Jugar", new { id = partida.par_id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al iniciar la partida: " + ex.Message;
                return RedirectToAction("Create");
            }
        }

        // GET: Partida/Jugar/5
        public ActionResult Jugar(int id)
        {
            var partida = db.Partidas
                .Include(p => p.Usuario)
                .Include(p => p.Palabra)
                .Include(p => p.Letras)
                .FirstOrDefault(p => p.par_id == id);

            if (partida == null)
            {
                return HttpNotFound();
            }

            // Verificar si la partida ya terminó
            if (partida.par_resultado != "En Progreso")
            {
                return RedirectToAction("Resultado", new { id = id });
            }

            // Preparar el modelo para la vista
            var modelo = new PartidaViewModel
            {
                PartidaId = partida.par_id,
                Usuario = partida.Usuario.usu_nombre,
                UsuarioId = partida.Usuario.usu_id,
                Nivel = partida.par_nivel,
                Palabra = partida.Palabra.pal_texto,
                PalabraOculta = ObtenerPalabraOculta(partida),
                LetrasUsadas = partida.Letras.Select(l => l.let_letra.ToUpper()).ToList(),
                IntentosRestantes = 5 - partida.Letras.Count(l => l.let_escorrecta == false),
                TiempoLimite = ObtenerTiempoLimite(partida.par_nivel),
                LetrasDisponibles = GenerarLetrasDisponibles(partida.Letras.Select(l => l.let_letra).ToList())
            };

            return View(modelo);
        }

        // POST: Partida/SeleccionarLetra
        [HttpPost]
        public JsonResult SeleccionarLetra(int partidaId, string letra)
        {
            try
            {
                var partida = db.Partidas
                    .Include(p => p.Palabra)
                    .Include(p => p.Letras)
                    .FirstOrDefault(p => p.par_id == partidaId);

                if (partida == null || partida.par_resultado != "En Progreso")
                {
                    return Json(new { success = false, message = "Partida no encontrada o ya finalizada." });
                }

                letra = letra.ToUpper();

                // Verificar si la letra ya fue usada
                if (partida.Letras.Any(l => l.let_letra.ToUpper() == letra))
                {
                    return Json(new { success = false, message = "Esta letra ya fue seleccionada." });
                }

                // Verificar si la letra está en la palabra (sin considerar tildes)
                string palabraNormalizada = RemoverTildes(partida.Palabra.pal_texto.ToUpper());
                bool esCorrecta = palabraNormalizada.Contains(letra);

                // Guardar la letra seleccionada
                var letraEntity = new Letra
                {
                    par_id = partidaId,
                    let_letra = letra,
                    let_escorrecta = esCorrecta
                };

                db.Letras.Add(letraEntity);
                db.SaveChanges();

                // Actualizar información del juego
                partida = db.Partidas
                    .Include(p => p.Palabra)
                    .Include(p => p.Letras)
                    .FirstOrDefault(p => p.par_id == partidaId);

                var palabraOculta = ObtenerPalabraOculta(partida);
                var intentosRestantes = 5 - partida.Letras.Count(l => l.let_escorrecta == false);
                var juegoTerminado = false;
                var gano = false;

                // Verificar condiciones de fin de juego
                if (!palabraOculta.Contains("_"))
                {
                    // Ganó
                    juegoTerminado = true;
                    gano = true;
                    partida.par_resultado = "Ganada";
                }
                else if (intentosRestantes <= 0)
                {
                    // Perdió por intentos
                    juegoTerminado = true;
                    partida.par_resultado = "Perdida";
                }

                if (juegoTerminado)
                {
                    db.SaveChanges();

                    // Actualizar puntaje del usuario
                    var usuarioController = new UsuarioController();
                    usuarioController.ActualizarPuntaje(partida.usu_id, partida.par_nivel, gano);
                }

                return Json(new
                {
                    success = true,
                    esCorrecta = esCorrecta,
                    palabraOculta = palabraOculta,
                    intentosRestantes = intentosRestantes,
                    juegoTerminado = juegoTerminado,
                    gano = gano,
                    palabraCompleta = partida.Palabra.pal_texto
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // POST: Partida/TerminarPorTiempo
        [HttpPost]
        public JsonResult TerminarPorTiempo(int partidaId)
        {
            try
            {
                var partida = db.Partidas.Find(partidaId);
                if (partida != null && partida.par_resultado == "En Progreso")
                {
                    partida.par_resultado = "Perdida";
                    db.SaveChanges();

                    // Actualizar puntaje del usuario
                    var usuarioController = new UsuarioController();
                    usuarioController.ActualizarPuntaje(partida.usu_id, partida.par_nivel, false);

                    return Json(new { success = true });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        // GET: Partida/Resultado/5
        public ActionResult Resultado(int id)
        {
            var partida = db.Partidas
                .Include(p => p.Usuario)
                .Include(p => p.Palabra)
                .Include(p => p.Letras)
                .FirstOrDefault(p => p.par_id == id);

            if (partida == null)
            {
                return HttpNotFound();
            }

            var modelo = new ResultadoViewModel
            {
                PartidaId = partida.par_id,
                Usuario = partida.Usuario.usu_nombre,
                UsuarioId = partida.Usuario.usu_id,
                Nivel = partida.par_nivel,
                Resultado = partida.par_resultado,
                Palabra = partida.Palabra.pal_texto,
                LetrasUsadas = partida.Letras.Select(l => l.let_letra).ToList(),
                IntentosUsados = partida.Letras.Count(l => l.let_escorrecta == false),
                Fecha = partida.par_fecha ?? DateTime.Now
            };

            return View(modelo);
        }

        // GET: Partida/NuevoIntento
        public ActionResult NuevoIntento(int usuarioId, string nivel)
        {
            return RedirectToAction("IniciarJuego", new { usuarioId = usuarioId, nivel = nivel });
        }

        // Métodos auxiliares
        private string ObtenerPalabraOculta(Partida partida)
        {
            var palabra = partida.Palabra.pal_texto.ToUpper();
            var letrasUsadas = partida.Letras.Where(l => l.let_escorrecta == true)
                                           .Select(l => l.let_letra.ToUpper()).ToList();

            var resultado = new StringBuilder();
            foreach (char c in palabra)
            {
                string cNormalizado = RemoverTildes(c.ToString());
                if (letrasUsadas.Any(l => l == cNormalizado))
                {
                    resultado.Append(c);
                }
                else
                {
                    resultado.Append("_");
                }
                resultado.Append(" ");
            }

            return resultado.ToString().Trim();
        }

        private int ObtenerTiempoLimite(string nivel)
        {
            switch (nivel.ToLower())
            {
                case "fácil":
                case "facil":
                    return 90; // 1.5 minutos
                case "normal":
                    return 60; // 1 minuto
                case "difícil":
                case "dificil":
                    return 30; // 0.5 minutos
                default:
                    return 60;
            }
        }

        private List<string> GenerarLetrasDisponibles(List<string> letrasUsadas)
        {
            var todasLasLetras = new List<string>
            {
                "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
                "N", "Ñ", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"
            };

            return todasLasLetras.Where(l => !letrasUsadas.Contains(l.ToUpper())).ToList();
        }

        private string RemoverTildes(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return texto;

            var textoNormalizado = texto.Normalize(NormalizationForm.FormD);
            var resultado = new StringBuilder();

            foreach (char c in textoNormalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    resultado.Append(c);
                }
            }

            return resultado.ToString().Normalize(NormalizationForm.FormC);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // ViewModels
    public class PartidaViewModel
    {
        public int PartidaId { get; set; }
        public string Usuario { get; set; }
        public int UsuarioId { get; set; }
        public string Nivel { get; set; }
        public string Palabra { get; set; }
        public string PalabraOculta { get; set; }
        public List<string> LetrasUsadas { get; set; }
        public List<string> LetrasDisponibles { get; set; }
        public int IntentosRestantes { get; set; }
        public int TiempoLimite { get; set; }
    }

    public class ResultadoViewModel
    {
        public int PartidaId { get; set; }
        public string Usuario { get; set; }
        public int UsuarioId { get; set; }
        public string Nivel { get; set; }
        public string Resultado { get; set; }
        public string Palabra { get; set; }
        public List<string> LetrasUsadas { get; set; }
        public int IntentosUsados { get; set; }
        public DateTime Fecha { get; set; }
    }
}