﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using DrinkDatabase.Models;

namespace DrinkDatabase.Controllers
{
    [Authorize]
    public class DrinkController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Drink
        [AllowAnonymous]
        public async Task<ActionResult> Index()
        {
            return View(await db.Drinks.ToListAsync());
        }

        // GET: Drink/Details/5
        [AllowAnonymous]
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Drink drink = await db.Drinks.Include(d => d.DrinkIngredients).Where(d => d.ID == id).SingleAsync();
            if (drink == null)
            {
                return HttpNotFound();
            }
            
            ViewBag.DrinkIngredients = drink.DrinkIngredients;
            return View(drink);
        }

        // GET: Drink/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Drink/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "ID,Name,Instructions,Glass,Notes")] Drink drink)
        {
            if (ModelState.IsValid)
            {
                db.Drinks.Add(drink);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(drink);
        }

        // GET: Drink/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Drink drinkToUpdate = await db.Drinks
                .Include(d => d.DrinkIngredients)
                .Where(d => d.ID == id)
                .SingleAsync();
            if (drinkToUpdate == null)
            {
                return HttpNotFound();
            }
            if (drinkToUpdate.DrinkIngredients == null)
            {
                drinkToUpdate.DrinkIngredients = new HashSet<DrinkIngredient>();
            }
            ViewBag.DrinkIngredients = drinkToUpdate.DrinkIngredients;
            return View(drinkToUpdate);
        }

        // POST: Drink/Edit/n
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "ID,Name,Instructions,Glass,Notes")] Drink drink,
            [Bind(Include= "ID,Amount,Brand,IngredientID,DrinkID")] IEnumerable<DrinkIngredient> DrinkIngredients)
        {
            if (ModelState.IsValid)
            {
                var whichToDelete = Request.Form.AllKeys.Where(s => s.Contains("DeleteDrinkIngredients["));
                List<int> deletionList = new List<int>();
                foreach (var s in whichToDelete)
                {
                    var firstBracket = s.IndexOf('[') + 1;
                    int thisIndex;
                    if (int.TryParse(s.Substring(firstBracket, s.IndexOf(']', firstBracket) - firstBracket), out thisIndex))
                    {
                        if(Request.Form[s] == "on")
                            deletionList.Add(thisIndex);
                    }
                }

                db.Entry(drink).State = EntityState.Modified;
                if (DrinkIngredients != null)
                {
                    foreach (var item in DrinkIngredients)
                    {
                        if(deletionList.Contains(item.ID))
                        {
                            drink.DrinkIngredients.Remove(item);
                            db.Entry(item).State = EntityState.Deleted;
                        }
                        else
                        {
                            db.Entry(item).State = EntityState.Modified;
                        }
                    }
                }
                await db.SaveChangesAsync();


                if (Request.IsAjaxRequest())
                {
                    ViewBag.DrinkIngredients = drink.DrinkIngredients;
                    return PartialView("_EditableDrink", drink);
                }
                return RedirectToAction("Index");
            }
            return View(drink);
        }

        // GET: Drink/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Drink drink = await db.Drinks.FindAsync(id);
            if (drink == null)
            {
                return HttpNotFound();
            }
            return View(drink);
        }

        // POST: Drink/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Drink drink = await db.Drinks.FindAsync(id);
            db.Drinks.Remove(drink);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        public async Task<ActionResult> AddIngredient(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Drink drink = await db.Drinks.FindAsync(id);
            if (drink == null)
            {
                return HttpNotFound();
            }
            var ingredients = db.Ingredients.OrderBy(q => q.Name).ToList();
            SelectList holdThis = new SelectList(ingredients, "ID", "Name", null);
            ViewData.Add("ingredientID", holdThis.AsEnumerable());

            return View(drink);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddIngredient(int id, int ingredientID)
        {
            Drink drink = await db.Drinks.FindAsync(id);
            if (drink == null)
                return HttpNotFound();

            Ingredient ingredient = await db.Ingredients.FirstAsync(i => i.ID == ingredientID);
            if (ingredient == null)
                return HttpNotFound();

            if (drink.DrinkIngredients.Any(di => di.IngredientID == ingredient.ID))
                return new HttpStatusCodeResult(HttpStatusCode.Conflict);

            drink.DrinkIngredients.Add(new DrinkIngredient()
            {
                DrinkID = id,
                IngredientID = ingredient.ID
            });
            await db.SaveChangesAsync();

            return RedirectToAction("Edit/" + id);
        }
        [AllowAnonymous]
        public ActionResult DrinkIngredientDetails(int? id)
        {
            if (id == null)
                return HttpNotFound();
            DrinkIngredient di = db.DrinkIngredients.Find(id);
            if (di == null)
                return HttpNotFound();
            
            var ingredient = db.Ingredients.Find(di.IngredientID);
            if (ingredient == null)
                return HttpNotFound();
            ViewBag.ingredientName = ingredient.Name;

            return PartialView(di);
        }

        public ActionResult DrinkIngredientEdit(int? id)
        {
            if (id == null)
                return HttpNotFound();
            DrinkIngredient di = db.DrinkIngredients.Find(id);
            if (di == null)
                return HttpNotFound();

            var ingredient = db.Ingredients.Find(di.IngredientID);
            if (ingredient == null)
                return HttpNotFound();

            var ingredients = db.Ingredients.OrderBy(q => q.Name).ToList();
            SelectList holdThis = new SelectList(ingredients, "ID", "Name", di.IngredientID);
            ViewData.Add("DrinkIngredients[" + id + "].ingredientID", holdThis.AsEnumerable());
            
            return PartialView(di);
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
