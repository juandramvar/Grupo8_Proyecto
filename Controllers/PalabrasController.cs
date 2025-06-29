using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Grupo8_Proyecto;

namespace Grupo8_Proyecto.Controllers
{
    public class PalabrasController : Controller
    {
        private AhorcadoContext db = new AhorcadoContext();

        // GET: Palabras
        public ActionResult Index()
        {
            return View(db.Palabras.ToList());
        }

        // GET: Palabras/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Palabra palabra = db.Palabras.Find(id);
            if (palabra == null)
            {
                return HttpNotFound();
            }
            return View(palabra);
        }

        // GET: Palabras/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Palabras/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "pal_texto")] Palabra palabra)
        {
            if (string.IsNullOrWhiteSpace(palabra.pal_texto))
            {
                ModelState.AddModelError("pal_texto", "Debe ingresar una palabra.");
                return View(palabra);
            }

            string textoOriginal = palabra.pal_texto.Trim();
            string textoNormalizado = textoOriginal
                .ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

            // Remueve tildes para comparación
            string textoSinTildes = new string(textoNormalizado
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray());

            // Validar longitud
            if (textoSinTildes.Length < 5 || textoSinTildes.Length > 10)
            {
                ModelState.AddModelError("pal_texto", "La palabra debe tener entre 5 y 10 letras.");
                return View(palabra);
            }

            // Verificar si ya existe una palabra equivalente (sin tildes y sin importar mayúsculas)
            var palabrasExistentes = db.Palabras.ToList();
            bool existe = palabrasExistentes.Any(p =>
                new string(p.pal_texto
                    .ToLowerInvariant()
                    .Normalize(NormalizationForm.FormD)
                    .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    .ToArray()) == textoSinTildes
            );

            if (existe)
            {
                ModelState.AddModelError("pal_texto", "Esta palabra ya existe en el diccionario.");
                return View(palabra);
            }

            // Guardar datos procesados
            palabra.pal_texto = textoOriginal;
            palabra.pal_longitud = textoSinTildes.Length;
            palabra.pal_tilde = textoOriginal.Any(c => "áéíóúÁÉÍÓÚ".Contains(c)) ? true : false;
            palabra.pal_inicial = char.ToUpper(textoSinTildes[0]).ToString();

            if (ModelState.IsValid)
            {
                db.Palabras.Add(palabra);
                db.SaveChanges();
                TempData["SuccessMessage"] = "¡La palabra fue agregada correctamente!";
                return RedirectToAction("Index");
            }

            return View(palabra);
        }

        // GET: Palabras/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Palabra palabra = db.Palabras.Find(id);
            if (palabra == null)
            {
                return HttpNotFound();
            }
            return View(palabra);
        }

        // POST: Palabras/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "pal_id,pal_texto,pal_longitud,pal_tilde,pal_inicial")] Palabra palabra)
        {
            if (ModelState.IsValid)
            {
                db.Entry(palabra).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(palabra);
        }

        // GET: Palabras/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Palabra palabra = db.Palabras.Find(id);
            if (palabra == null)
            {
                return HttpNotFound();
            }
            return View(palabra);
        }

        // POST: Palabras/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Palabra palabra = db.Palabras.Find(id);
            db.Palabras.Remove(palabra);
            db.SaveChanges();
            return RedirectToAction("Index");
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
}
