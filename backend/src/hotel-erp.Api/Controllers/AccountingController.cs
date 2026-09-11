using AutoMapper;
using hotel_erp.Api.Authorization;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Dtos.Accounting;
using hotel_erp.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = PermissionNames.ManageAccounting)]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountingRepository _repo;
        private readonly IMapper _mapper;

        public AccountsController(IAccountingRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<AccountingAccountDto>>> GetAll()
        {
            var accounts = await _repo.GetAllAccountsAsync();
            var balances = await _repo.GetAllAccountBalancesAsync();
            var dtos = _mapper.Map<List<AccountingAccountDto>>(accounts);
            foreach (var dto in dtos)
                dto.Balance = balances.GetValueOrDefault(dto.Id, 0);
            var tree = BuildTree(dtos, null);
            return Ok(tree);
        }

        [HttpGet("flat")]
        public async Task<ActionResult<List<AccountingAccountDto>>> GetAllFlat()
        {
            var accounts = await _repo.GetAllAccountsAsync();
            var balances = await _repo.GetAllAccountBalancesAsync();
            var dtos = _mapper.Map<List<AccountingAccountDto>>(accounts);
            foreach (var dto in dtos)
                dto.Balance = balances.GetValueOrDefault(dto.Id, 0);
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AccountingAccountDto>> GetById(Guid id)
        {
            var account = await _repo.GetAccountByIdAsync(id);
            if (account == null) return NotFound();
            var dto = _mapper.Map<AccountingAccountDto>(account);
            dto.Balance = await _repo.GetAccountBalanceAsync(id);
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<AccountingAccountDto>> Create([FromBody] CreateAccountRequest request)
        {
            if (await _repo.AccountNumberExistsAsync(request.AccountNumber))
                return BadRequest("El número de cuenta ya existe");

            if (!Enum.TryParse<AccountType>(request.AccountType, out var accountType))
                return BadRequest("Tipo de cuenta inválido. Use: Activo, Pasivo, Patrimonio, Ingreso, Gasto");

            var account = new AccountingAccount
            {
                AccountNumber = request.AccountNumber,
                AccountName = request.AccountName,
                AccountType = accountType,
                ParentAccountId = request.ParentAccountId,
                IsActive = true
            };

            await _repo.AddAccountAsync(account);
            var dto = _mapper.Map<AccountingAccountDto>(account);
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] UpdateAccountRequest request)
        {
            var account = await _repo.GetAccountByIdAsync(id);
            if (account == null) return NotFound();

            if (request.AccountName != null)
                account.AccountName = request.AccountName;

            if (request.AccountType != null)
            {
                if (!Enum.TryParse<AccountType>(request.AccountType, out var accountType))
                    return BadRequest("Tipo de cuenta inválido");
                account.AccountType = accountType;
            }

            if (request.IsActive.HasValue)
                account.IsActive = request.IsActive.Value;

            account.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAccountAsync(account);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Deactivate(Guid id)
        {
            var account = await _repo.GetAccountByIdAsync(id);
            if (account == null) return NotFound();

            var movements = await _repo.GetAccountMovementsAsync(id);
            if (movements.Any())
            {
                account.IsActive = false;
                account.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAccountAsync(account);
                return Ok(new { message = "Cuenta desactivada (tiene movimientos)" });
            }

            await _repo.DeleteAccountAsync(account);
            return Ok(new { message = "Cuenta eliminada" });
        }

        [HttpGet("journal-entries")]
        public async Task<ActionResult<List<AccountingEntryDto>>> GetJournalEntries()
        {
            var entries = await _repo.GetAllEntriesAsync();
            return Ok(_mapper.Map<List<AccountingEntryDto>>(entries));
        }

        [HttpGet("journal-entries/{id}")]
        public async Task<ActionResult<AccountingEntryDto>> GetJournalEntry(Guid id)
        {
            var entry = await _repo.GetEntryByIdAsync(id);
            if (entry == null) return NotFound();
            return Ok(_mapper.Map<AccountingEntryDto>(entry));
        }

        [HttpPost("journal-entries")]
        public async Task<ActionResult<AccountingEntryDto>> CreateJournalEntry([FromBody] CreateAccountingEntryRequest request)
        {
            if (request.Items == null || request.Items.Count < 2)
                return BadRequest(new { message = "Un asiento contable requiere al menos 2 cuentas (partida doble)" });

            var totalDebit = request.Items.Sum(i => i.Debit);
            var totalCredit = request.Items.Sum(i => i.Credit);

            if (totalDebit <= 0 || totalCredit <= 0)
                return BadRequest(new { message = "Los importes del asiento deben ser mayores a cero" });

            if (Math.Round(totalDebit, 2) != Math.Round(totalCredit, 2))
                return BadRequest(new { message = $"El asiento está descuadrado. Débitos: L {totalDebit:N2} vs Créditos: L {totalCredit:N2}" });

            var accounts = await _repo.GetAllAccountsAsync();
            var accountDict = accounts.ToDictionary(a => a.Id);

            var entryType = Enum.TryParse<EntryType>(request.EntryType, true, out var parsedType)
                ? parsedType
                : EntryType.Diario;

            var entry = new AccountingEntry
            {
                TransactionDate = request.TransactionDate,
                Description = request.Description.Trim(),
                EntryType = entryType,
                ReferenceId = null,
            };

            foreach (var item in request.Items)
            {
                if (!accountDict.ContainsKey(item.AccountId))
                    return BadRequest(new { message = $"La cuenta contable {item.AccountId} no existe" });

                if (item.Debit > 0 && item.Credit > 0)
                    return BadRequest(new { message = "Una línea contable no puede tener débito y crédito simultáneamente" });

                entry.EntryItems.Add(new EntryItem
                {
                    AccountId = item.AccountId,
                    Debit = item.Debit,
                    Credit = item.Credit,
                    Description = item.Description?.Trim()
                });
            }

            await _repo.AddEntryAsync(entry);

            var createdEntry = await _repo.GetEntryByIdAsync(entry.Id);
            return CreatedAtAction(nameof(GetJournalEntry), new { id = entry.Id }, _mapper.Map<AccountingEntryDto>(createdEntry ?? entry));
        }


        [HttpGet("trial-balance")]
        public async Task<ActionResult<List<TrialBalanceItem>>> GetTrialBalance()
        {
            var accounts = await _repo.GetAllAccountsAsync();
            var accountTotals = await _repo.GetAccountTotalsAsync();
            var items = new List<TrialBalanceItem>();

            foreach (var account in accounts.Where(a => a.IsActive))
            {
                var totals = accountTotals.TryGetValue(account.Id, out var t) ? t : (Debit: 0m, Credit: 0m);
                var totalDebit = totals.Debit;
                var totalCredit = totals.Credit;
                var balance = account.AccountType switch
                {
                    AccountType.Activo or AccountType.Gasto => totalDebit - totalCredit,
                    _ => totalCredit - totalDebit,
                };

                items.Add(new TrialBalanceItem
                {
                    AccountId = account.Id,
                    AccountNumber = account.AccountNumber,
                    AccountName = account.AccountName,
                    AccountType = account.AccountType.ToString(),
                    PreviousBalance = 0,
                    Debit = totalDebit,
                    Credit = totalCredit,
                    Balance = balance
                });
            }

            return Ok(items.OrderBy(i => i.AccountNumber).ToList());
        }

        [HttpGet("{id}/movements")]
        public async Task<ActionResult<List<AccountMovementDto>>> GetMovements(Guid id)
        {
            var account = await _repo.GetAccountByIdAsync(id);
            if (account == null) return NotFound();

            var movements = await _repo.GetAccountMovementsAsync(id);
            var runningBalance = 0m;

            var dtos = movements.Select(m =>
            {
                var diff = account.AccountType switch
                {
                    AccountType.Activo or AccountType.Gasto => m.Debit - m.Credit,
                    _ => m.Credit - m.Debit,
                };
                runningBalance += diff;
                return new AccountMovementDto
                {
                    Date = m.Entry.TransactionDate.ToDateTime(TimeOnly.MinValue),
                    Description = m.Description ?? m.Entry.Description,
                    Debit = m.Debit,
                    Credit = m.Credit,
                    Balance = runningBalance,
                    ReferenceType = m.Entry.EntryType.ToString()
                };
            }).ToList();

            return Ok(dtos);
        }

        private static List<AccountingAccountDto> BuildTree(List<AccountingAccountDto> flat, Guid? parentId)
        {
            return flat
                .Where(a => a.ParentAccountId == parentId)
                .Select(a =>
                {
                    a.Children = BuildTree(flat, a.Id);
                    return a;
                })
                .ToList();
        }
    }
}
