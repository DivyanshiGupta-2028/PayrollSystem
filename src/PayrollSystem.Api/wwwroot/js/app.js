// ============================================================================
// File: app.js
// Description: Client-side logic for Apex Payroll Run Module.
// ============================================================================

const API_BASE = '/api';

let currentPayrollData = null;
let currentRunId = null;
let allEmployees = [];
let currentPage = 1;
const pageSize = 10;
let filteredItems = [];

// Initialize application on page load
document.addEventListener('DOMContentLoaded', () => {
  loadPayrollHistory();
  loadEmployees();
});

// ----------------------------------------------------------------------------
// Navigation / Tab Switching
// ----------------------------------------------------------------------------
function switchTab(tab) {
  const payrollTabBtn = document.getElementById('tabPayroll');
  const employeesTabBtn = document.getElementById('tabEmployees');
  const payrollContent = document.getElementById('payrollTabContent');
  const employeesContent = document.getElementById('employeesTabContent');

  if (tab === 'payroll') {
    payrollTabBtn.classList.add('active');
    employeesTabBtn.classList.remove('active');
    payrollContent.style.display = 'block';
    employeesContent.style.display = 'none';
  } else {
    employeesTabBtn.classList.add('active');
    payrollTabBtn.classList.remove('active');
    payrollContent.style.display = 'none';
    employeesContent.style.display = 'block';
    loadEmployees();
  }
}

// ----------------------------------------------------------------------------
// Alerts / Feedback
// ----------------------------------------------------------------------------
function showAlert(message, type = 'info') {
  const container = document.getElementById('alertContainer');
  const alertClass = type === 'conflict' ? 'alert-conflict' : (type === 'danger' ? 'alert-danger' : (type === 'success' ? 'alert-success' : 'alert-info'));
  const icon = type === 'conflict' ? '⚠️' : (type === 'danger' ? '❌' : (type === 'success' ? '✅' : 'ℹ️'));

  container.innerHTML = `
    <div class="alert ${alertClass}">
      <div><strong>${icon}</strong> ${escapeHtml(message)}</div>
      <button class="alert-close" onclick="this.parentElement.remove()">✕</button>
    </div>
  `;

  // Auto-scroll to alert if needed
  window.scrollTo({ top: 0, behavior: 'smooth' });
}

function clearAlert() {
  document.getElementById('alertContainer').innerHTML = '';
}

// ----------------------------------------------------------------------------
// API Calls: Run Payroll & Load Payroll
// ----------------------------------------------------------------------------
async function triggerPayrollRun() {
  clearAlert();
  const month = parseInt(document.getElementById('selectMonth').value, 10);
  const year = parseInt(document.getElementById('selectYear').value, 10);

  const btn = document.getElementById('btnRunPayroll');
  const btnText = document.getElementById('btnRunText');
  const originalText = btnText.innerHTML;

  btn.disabled = true;
  btnText.innerHTML = `<span class="spinner"></span> Processing...`;

  try {
    const response = await fetch(`${API_BASE}/payroll/run`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ month, year, createdBy: 'HR Admin' })
    });

    if (response.status === 201) {
      const data = await response.json();
      currentPayrollData = data;
      currentRunId = data.payrollRunId;
      showAlert(`Payroll for ${getMonthName(month)} ${year} was processed and finalized successfully! (Run #${data.payrollRunId})`, 'success');
      renderPayrollView(data);
      loadPayrollHistory();
    } else if (response.status === 409) {
      const err = await response.json();
      showAlert(`Conflict (409): ${err.message || 'Payroll for this month/year has already been finalized and is immutable.'}`, 'conflict');
      // Auto-load existing run so user can inspect it
      await fetchPayrollRun(month, year);
    } else if (response.status === 400) {
      const err = await response.json();
      showAlert(`Validation Error (400): ${err.message || 'Invalid request parameters.'}`, 'danger');
    } else {
      const err = await response.json();
      showAlert(`Error: ${err.message || 'Failed to execute payroll run.'}`, 'danger');
    }
  } catch (error) {
    showAlert(`Network error: ${error.message}`, 'danger');
  } finally {
    btn.disabled = false;
    btnText.innerHTML = originalText;
  }
}

async function fetchPayrollRun(explicitMonth = null, explicitYear = null) {
  clearAlert();
  const month = explicitMonth || parseInt(document.getElementById('selectMonth').value, 10);
  const year = explicitYear || parseInt(document.getElementById('selectYear').value, 10);

  const btn = document.getElementById('btnLoadPayroll');
  btn.disabled = true;

  try {
    const response = await fetch(`${API_BASE}/payroll/${month}/${year}`);

    if (response.status === 200) {
      const data = await response.json();
      currentPayrollData = data;
      currentRunId = data.payrollRunId;
      renderPayrollView(data);
      showAlert(`Loaded finalized payroll run #${data.payrollRunId} for ${getMonthName(month)} ${year}.`, 'info');
    } else if (response.status === 404) {
      hidePayrollView();
      showAlert(`No finalized payroll run found for ${getMonthName(month)} ${year}. You can trigger a new run using "Run Payroll".`, 'info');
    } else {
      const err = await response.json();
      showAlert(`Error: ${err.message}`, 'danger');
    }
  } catch (error) {
    showAlert(`Network error: ${error.message}`, 'danger');
  } finally {
    btn.disabled = false;
  }
}

async function loadPayrollHistory() {
  try {
    const response = await fetch(`${API_BASE}/payroll/history`);
    if (response.ok) {
      const runs = await response.json();
      const select = document.getElementById('historyRuns');
      select.innerHTML = '<option value="">-- Quick Select Finalized Run --</option>';

      runs.forEach(r => {
        const option = document.createElement('option');
        option.value = `${r.payrollMonth}_${r.payrollYear}`;
        option.textContent = `Run #${r.payrollRunId}: ${getMonthName(r.payrollMonth)} ${r.payrollYear} (${r.totalEmployees} employees - ₹${formatCurrency(r.totalNetPay)})`;
        select.appendChild(option);
      });
    }
  } catch (e) {
    console.error('Failed to load payroll history', e);
  }
}

function loadSelectedHistoricalRun() {
  const val = document.getElementById('historyRuns').value;
  if (!val) return;
  const [month, year] = val.split('_').map(Number);
  document.getElementById('selectMonth').value = month;
  document.getElementById('selectYear').value = year;
  fetchPayrollRun(month, year);
}

// ----------------------------------------------------------------------------
// Rendering Functions
// ----------------------------------------------------------------------------
function renderPayrollView(data) {
  document.getElementById('emptyPlaceholder').style.display = 'none';
  document.getElementById('metricsSection').style.display = 'grid';
  document.getElementById('resultsSection').style.display = 'block';

  // Render Metrics Cards
  document.getElementById('metricTotalEmployees').textContent = data.totalEmployees;
  document.getElementById('metricTotalGross').textContent = `₹${formatCurrency(data.totalGrossPay)}`;
  document.getElementById('metricTotalPF').textContent = `₹${formatCurrency(data.totalPFDeduction)}`;
  document.getElementById('metricTotalPT').textContent = `₹${formatCurrency(data.totalProfessionalTax)}`;
  document.getElementById('metricTotalNet').textContent = `₹${formatCurrency(data.totalNetPay)}`;
  document.getElementById('metricStatus').innerHTML = `Status: <span class="badge badge-finalized">${data.status}</span>`;

  // Subtitle
  document.getElementById('runDetailsSubtitle').textContent = 
    `Run ID: #${data.payrollRunId} | Period: ${getMonthName(data.month)} ${data.year} | Finalized on: ${formatDate(data.runDate)}`;

  // Populate department filter
  const departments = [...new Set(data.items.map(i => i.departmentName))];
  const deptFilter = document.getElementById('departmentFilter');
  deptFilter.innerHTML = '<option value="ALL">All Departments</option>';
  departments.forEach(dept => {
    deptFilter.innerHTML += `<option value="${escapeHtml(dept)}">${escapeHtml(dept)}</option>`;
  });

  filteredItems = [...data.items];
  currentPage = 1;
  renderTable();
}

function hidePayrollView() {
  document.getElementById('emptyPlaceholder').style.display = 'block';
  document.getElementById('metricsSection').style.display = 'none';
  document.getElementById('resultsSection').style.display = 'none';
}

function handleSearch() {
  applyFilters();
}

function handleFilter() {
  applyFilters();
}

function applyFilters() {
  if (!currentPayrollData || !currentPayrollData.items) return;

  const query = document.getElementById('searchInput').value.trim().toLowerCase();
  const dept = document.getElementById('departmentFilter').value;

  filteredItems = currentPayrollData.items.filter(item => {
    const matchesQuery = !query || 
      item.name.toLowerCase().includes(query) ||
      item.employeeCode.toLowerCase().includes(query) ||
      item.designation.toLowerCase().includes(query);

    const matchesDept = dept === 'ALL' || item.departmentName === dept;

    return matchesQuery && matchesDept;
  });

  currentPage = 1;
  renderTable();
}

function renderTable() {
  const tbody = document.getElementById('payrollTableBody');
  tbody.innerHTML = '';

  const total = filteredItems.length;
  const totalPages = Math.ceil(total / pageSize) || 1;
  const startIndex = (currentPage - 1) * pageSize;
  const endIndex = Math.min(startIndex + pageSize, total);
  const pageItems = filteredItems.slice(startIndex, endIndex);

  if (pageItems.length === 0) {
    tbody.innerHTML = `<tr><td colspan="11" style="text-align:center; padding: 24px; color: var(--text-muted);">No matching employee payroll records found.</td></tr>`;
  } else {
    pageItems.forEach(item => {
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td class="font-mono">${escapeHtml(item.employeeCode)}</td>
        <td>
          <div style="font-weight: 600;">${escapeHtml(item.name)}</div>
          <div style="font-size: 11px; color: var(--text-muted);">ID: ${item.employeeId}</div>
        </td>
        <td>
          <span class="badge badge-dept">${escapeHtml(item.departmentName)}</span>
          <div style="font-size: 11px; color: var(--text-muted); margin-top:2px;">${escapeHtml(item.designation)}</div>
        </td>
        <td class="text-right font-mono">₹${formatCurrency(item.basicSalary)}</td>
        <td class="text-right font-mono">${item.workingDays}</td>
        <td class="text-right font-mono">${item.daysPresent}</td>
        <td class="text-right font-mono" style="font-weight:600;">₹${formatCurrency(item.grossPay)}</td>
        <td class="text-right font-mono" style="color: var(--danger);">₹${formatCurrency(item.pfDeduction)}</td>
        <td class="text-right font-mono" style="color: var(--danger);">₹${formatCurrency(item.professionalTax)}</td>
        <td class="text-right font-mono" style="font-weight:700; color: var(--success); font-size:14px;">₹${formatCurrency(item.netPay)}</td>
        <td class="text-center">
          <button class="btn btn-secondary btn-sm" onclick="viewPayslip(${currentRunId}, ${item.employeeId})">
            📄 Payslip
          </button>
        </td>
      `;
      tbody.appendChild(tr);
    });
  }

  // Update Pagination Controls
  document.getElementById('paginationInfo').textContent = 
    total > 0 ? `Showing ${startIndex + 1} to ${endIndex} of ${total} employees` : 'Showing 0 employees';
  document.getElementById('pageIndicator').textContent = `${currentPage} / ${totalPages}`;
  document.getElementById('btnPrevPage').disabled = currentPage <= 1;
  document.getElementById('btnNextPage').disabled = currentPage >= totalPages;
}

function changePage(direction) {
  const totalPages = Math.ceil(filteredItems.length / pageSize) || 1;
  const newPage = currentPage + direction;
  if (newPage >= 1 && newPage <= totalPages) {
    currentPage = newPage;
    renderTable();
  }
}

// ----------------------------------------------------------------------------
// Payslip Modal & Printable View
// ----------------------------------------------------------------------------
async function viewPayslip(runId, employeeId) {
  try {
    const response = await fetch(`${API_BASE}/payroll/${runId}/slip/${employeeId}`);
    if (!response.ok) {
      const err = await response.json();
      showAlert(`Failed to fetch payslip: ${err.message}`, 'danger');
      return;
    }

    const slip = await response.json();

    document.getElementById('slipPeriodTitle').textContent = 
      `SALARY PAYSLIP FOR ${getMonthName(slip.month).toUpperCase()} ${slip.year}`;
    document.getElementById('slipEmpCode').textContent = slip.employeeCode;
    document.getElementById('slipEmpName').textContent = slip.name;
    document.getElementById('slipDept').textContent = slip.departmentName;
    document.getElementById('slipDesignation').textContent = slip.designation;
    document.getElementById('slipDoj').textContent = formatDateOnly(slip.dateOfJoining);
    document.getElementById('slipRunId').textContent = `#${slip.payrollRunId}`;
    document.getElementById('slipWorkingDays').textContent = slip.workingDays;
    document.getElementById('slipDaysPresent').textContent = `${slip.daysPresent} (Absent: ${slip.daysAbsent})`;

    document.getElementById('slipBasic').textContent = `₹${formatCurrency(slip.basicSalary)}`;
    document.getElementById('slipGross').textContent = `₹${formatCurrency(slip.grossPay)}`;
    document.getElementById('slipPF').textContent = `₹${formatCurrency(slip.pfDeduction)}`;
    document.getElementById('slipPT').textContent = `₹${formatCurrency(slip.professionalTax)}`;
    document.getElementById('slipTotalGross').textContent = `₹${formatCurrency(slip.grossPay)}`;
    document.getElementById('slipTotalDeductions').textContent = `₹${formatCurrency(slip.totalDeductions)}`;

    document.getElementById('slipNet').textContent = `₹${formatCurrency(slip.netPay)}`;
    document.getElementById('slipWords').textContent = slip.netPayInWords;
    document.getElementById('slipGenDate').textContent = formatDate(slip.runDate);

    document.getElementById('payslipModal').style.display = 'flex';
  } catch (error) {
    showAlert(`Error loading payslip: ${error.message}`, 'danger');
  }
}

function closePayslipModal() {
  document.getElementById('payslipModal').style.display = 'none';
}

function printPayslip() {
  window.print();
}

// ----------------------------------------------------------------------------
// Employee Directory Tab
// ----------------------------------------------------------------------------
async function loadEmployees() {
  try {
    const response = await fetch(`${API_BASE}/employees`);
    if (response.ok) {
      allEmployees = await response.json();
      const tbody = document.getElementById('employeeMasterTableBody');
      tbody.innerHTML = '';

      allEmployees.forEach(emp => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
          <td class="font-mono">${escapeHtml(emp.employeeCode)}</td>
          <td style="font-weight: 600;">${escapeHtml(emp.name)}</td>
          <td><span class="badge badge-dept">${escapeHtml(emp.departmentName)}</span></td>
          <td>${escapeHtml(emp.designation)}</td>
          <td class="text-right font-mono">₹${formatCurrency(emp.basicSalary)}</td>
          <td>${formatDateOnly(emp.dateOfJoining)}</td>
          <td><span class="badge ${emp.isActive ? 'badge-finalized' : 'badge-dept'}">${emp.isActive ? 'Active' : 'Inactive'}</span></td>
        `;
        tbody.appendChild(tr);
      });
    }
  } catch (e) {
    console.error('Failed to load employees', e);
  }
}

// ----------------------------------------------------------------------------
// Utility Helpers
// ----------------------------------------------------------------------------
function formatCurrency(amount) {
  if (amount === undefined || amount === null) return '0';
  return Math.round(amount).toLocaleString('en-IN');
}

function formatDate(dateStr) {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleDateString('en-IN', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
}

function formatDateOnly(dateStr) {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleDateString('en-IN', {
    day: '2-digit',
    month: 'short',
    year: 'numeric'
  });
}

function getMonthName(monthNum) {
  const months = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
  return months[monthNum - 1] || `Month ${monthNum}`;
}

function escapeHtml(str) {
  if (!str) return '';
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

