using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Eagle.DataContexts;
using Eagle.Models;
using Eagle.DbTables;
using Eagle.DbExtensions;

namespace Eagle.Core.Taxonomy
{
    public class TaxonomyTermController : CoreController
    {
        // GET: api/TaxonomyTerm
        [HttpGet]
        public IEnumerable<TaxonomyTermEntity> GetTaxonomyTerms()
        {
            return dc.TaxonomyTerms;
        }

        // GET: api/TaxonomyTerm/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaxonomyTermEntity([FromRoute] string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var taxonomyTermEntity = await dc.TaxonomyTerms.SingleOrDefaultAsync(m => m.Id == id);

            if (taxonomyTermEntity == null)
            {
                return NotFound();
            }

            return Ok(taxonomyTermEntity);
        }

        // PUT: api/TaxonomyTerm/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTaxonomyTermEntity([FromRoute] string id, [FromBody] TaxonomyTermEntity taxonomyTermEntity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != taxonomyTermEntity.Id)
            {
                return BadRequest();
            }

            dc.Entry(taxonomyTermEntity).State = EntityState.Modified;

            try
            {
                await dc.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaxonomyTermEntityExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/TaxonomyTerm
        [HttpPost]
        public async Task<IActionResult> PostTaxonomyTermEntity([FromBody] TaxonomyTermEntity taxonomyTermEntity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            dc.TaxonomyTerms.Add(taxonomyTermEntity);
            await dc.SaveChangesAsync();

            return CreatedAtAction("GetTaxonomyTermEntity", new { id = taxonomyTermEntity.Id }, taxonomyTermEntity);
        }

        // DELETE: api/TaxonomyTerm/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTaxonomyTermEntity([FromRoute] string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var taxonomyTermEntity = await dc.TaxonomyTerms.SingleOrDefaultAsync(m => m.Id == id);
            if (taxonomyTermEntity == null)
            {
                return NotFound();
            }

            dc.TaxonomyTerms.Remove(taxonomyTermEntity);
            await dc.SaveChangesAsync();

            return Ok(taxonomyTermEntity);
        }

        private bool TaxonomyTermEntityExists(string id)
        {
            return dc.TaxonomyTerms.Any(e => e.Id == id);
        }
    }
}