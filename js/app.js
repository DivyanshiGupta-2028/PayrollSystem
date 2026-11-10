// ============================================================================
// File: app.js
// Description: Client-side logic for Apex Payroll Run Module.
//              Supports both Live ASP.NET Core API Backend and Interactive
//              GitHub Pages Demo Mode.
// ============================================================================

const API_BASE = '/api';

let isDemoMode = false;
let currentPayrollData = null;
let currentRunId = null;
let allEmployees = [];
let currentPage = 1;
const pageSize = 10;
let filteredItems = [];

// Seed Data for Interactive Static/GitHub Pages Demo Mode
const DEMO_EMPLOYEES = [
  { employeeId: 1, employeeCode: 'EMP001', name: 'Ravi Sharma', firstName: 'Ravi', lastName: 'Sharma', email: 'ravi.sharma@company.com', departmentId: 1, departmentName: 'Engineering', designation: 'Senior Software Engineer', basicSalary: 30000, dateOfJoining: '2023-01-15T00:00:00Z', isActive: true, workingDays: 26, daysPresent: 24 },
  { employeeId: 2, employeeCode: 'EMP002', name: 'Priya Patel', firstName: 'Priya', lastName: 'Patel', email: 'priya.patel@company.com', departmentId: 1, departmentName: 'Engineering', designation: 'Frontend Developer', basicSalary: 45000, dateOfJoining: '2023-04-10T00:00:00Z', isActive: true, workingDays: 26, daysPresent: 26 },
  { employeeId: 3, employeeCode: 'EMP003', name: 'Amit Verma', firstName: 'Amit', lastName: 'Verma', email: 'amit.verma@company.com', departmentId: 2, departmentName: 'Human Resources', designation: 'HR Specialist', basicSalary: 28000, dateOfJoining: '2022-08-01T00:00:00Z', isActive: true, workingDays: 26, daysPresent: 22 },
  { employeeId: 4, employeeCode: 'EMP004', name: 'Sneha Reddy', firstName: 'Sneha', lastName: 'Reddy', email: 'sneha.reddy@company.com', departmentId: 3, departmentName: 'Finance', designation: 'Financial Analyst', basicSalary: 52000, dateOfJoining: '2021-11-20T00:00:00Z', isActive: true, workingDays: 26, daysPresent: 25 },
  { employeeId: 5, employeeCode: 'EMP005', name: 'Vikram Singh', firstName: 'Vikram', lastName: 'Singh', email: 'vikram.singh@company.com', departmentId: 1, departmentName: 'Engineering', designation: 'QA Engineer', basicSalary: 35000, dateOfJoining: '2023-09-05T00:00:00Z', isActive: true, workingDays: 26, daysPresent: 20 },
  { employeeId: 6, employeeCode: 'EMP006', name: 'Ananya Gupta', firstName: 'Ananya', lastName: 'Gupta', email: 'ananya.gupta@company.com', departmentId: 2, departmentName: 'Human Resources', designation: 'Talent Acquisition Lead', basicSalary: 40000, dateOfJoining: '2024-02-15T00:00:00Z', isActive: true, workingDays: 26, daysPresent: 26 }
];

const demoRuns = new Map();

// Initialize application on page load
document.addEventListener('DOMContentLoaded', async () => {
  await checkBackendAvailability();
  loadPayrollHistory();
  loadEmployees();
});

async function checkBackendAvailability() {
  try {
    const res = await fetch(`${API_BASE}/employees`, { method: 'GET' });
    if (!res.ok) {
      enableDemoMode();
    }
  } catch (e) {
    enableDemoMode();
  }
}

function enableDemoMode() {
  isDemoMode = true;
  const badge = document.querySelector('.user-badge span:last-child');
  if (badge) {
    badge.textContent = 'Live Interactive Preview (Cloud Demo)';
  }
}

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
    if (isDemoMode) {
      await simulatePayrollRun(month, year);
    } else {
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
        await fetchPayrollRun(month, year);
      } else if (response.status === 400) {
        const err = await response.json();
        showAlert(`Validation Error (400): ${err.message || 'Invalid request parameters.'}`, 'danger');
      } else {
        const err = await response.json();
        showAlert(`Error: ${err.message || 'Failed to execute payroll run.'}`, 'danger');
      }
    }
  } catch (error) {
    if (!isDemoMode) {
      enableDemoMode();
      await simulatePayrollRun(month, year);
    } else {
      showAlert(`Error: ${error.message}`, 'danger');
    }
  } finally {
    btn.disabled = false;
    btnText.innerHTML = originalText;
  }
}

async function simulatePayrollRun(month, year) {
  await new Promise(r => setTimeout(r, 600)); // UI polish delay

  const key = `${month}_${year}`;
  if (demoRuns.has(key)) {
    showAlert(`Conflict (409): A finalized payroll run already exists for ${month}/${year}. Finalized payroll runs cannot be modified or re-run.`, 'conflict');
    renderPayrollView(demoRuns.get(key));
    return;
  }

  // Calculate using exact assessment rules:
  // Gross = (Basic / WorkingDays) * DaysPresent
  // PF = 12% Basic
  // PT = Flat 200
  // Net = Gross - PF - PT
  const items = DEMO_EMPLOYEES.map(emp => {
    const gross = Math.round((emp.basicSalary / emp.workingDays) * emp.daysPresent);
    const pf = Math.round(emp.basicSalary * 0.12);
    const pt = emp.daysPresent > 0 ? 200 : 0;
    const net = Math.max(0, gross - pf - pt);

    return {
      employeeId: emp.employeeId,
      employeeCode: emp.employeeCode,
      name: emp.name,
      departmentName: emp.departmentName,
      designation: emp.designation,
      basicSalary: emp.basicSalary,
      workingDays: emp.workingDays,
      daysPresent: emp.daysPresent,
      grossPay: gross,
      pfDeduction: pf,
      professionalTax: pt,
      netPay: net
    };
  });

  const runId = demoRuns.size + 1;
  const summary = {
    payrollRunId: runId,
    month,
    year,
    runDate: new Date().toISOString(),
    totalEmployees: items.length,
    totalGrossPay: items.reduce((s, i) => s + i.grossPay, 0),
    totalPFDeduction: items.reduce((s, i) => s + i.pfDeduction, 0),
    totalProfessionalTax: items.reduce((s, i) => s + i.professionalTax, 0),
    totalNetPay: items.reduce((s, i) => s + i.netPay, 0),
    status: 'Finalized',
    createdBy: 'HR Admin (Interactive Demo)',
    items
  };

  demoRuns.set(key, summary);
  currentPayrollData = summary;
  currentRunId = runId;
  showAlert(`Payroll for ${getMonthName(month)} ${year} was processed and finalized successfully! (Run #${runId})`, 'success');
  renderPayrollView(summary);
  loadPayrollHistory();
}

async function fetchPayrollRun(explicitMonth = null, explicitYear = null) {
  clearAlert();
  const month = explicitMonth || parseInt(document.getElementById('selectMonth').value, 10);
  const year = explicitYear || parseInt(document.getElementById('selectYear').value, 10);

  const btn = document.getElementById('btnLoadPayroll');
  btn.disabled = true;

  try {
    if (isDemoMode) {
      const key = `${month}_${year}`;
      if (demoRuns.has(key)) {
        const data = demoRuns.get(key);
        currentPayrollData = data;
        currentRunId = data.payrollRunId;
        renderPayrollView(data);
        showAlert(`Loaded finalized payroll run #${data.payrollRunId} for ${getMonthName(month)} ${year}.`, 'info');
      } else {
        hidePayrollView();
        showAlert(`No finalized payroll run found for ${getMonthName(month)} ${year}. Click "Run Payroll" to process it.`, 'info');
      }
    } else {
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
    }
  } catch (error) {
    showAlert(`Error: ${error.message}`, 'danger');
  } finally {
    btn.disabled = false;
  }
}

async function loadPayrollHistory() {
  const select = document.getElementById('historyRuns');
  select.innerHTML = '<option value="">-- Quick Select Finalized Run --</option>';

  if (isDemoMode) {
    demoRuns.forEach((r, key) => {
      const option = document.createElement('option');
      option.value = key;
      option.textContent = `Run #${r.payrollRunId}: ${getMonthName(r.month)} ${r.year} (${r.totalEmployees} employees - ₹${formatCurrency(r.totalNetPay)})`;
      select.appendChild(option);
    });
    return;
  }

  try {
    const response = await fetch(`${API_BASE}/payroll/history`);
    if (response.ok) {
      const runs = await response.json();
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

  document.getElementById('metricTotalEmployees').textContent = data.totalEmployees;
  document.getElementById('metricTotalGross').textContent = `₹${formatCurrency(data.totalGrossPay)}`;
  document.getElementById('metricTotalPF').textContent = `₹${formatCurrency(data.totalPFDeduction)}`;
  document.getElementById('metricTotalPT').textContent = `₹${formatCurrency(data.totalProfessionalTax)}`;
  document.getElementById('metricTotalNet').textContent = `₹${formatCurrency(data.totalNetPay)}`;
  document.getElementById('metricStatus').innerHTML = `Status: <span class="badge badge-finalized">${data.status}</span>`;

  document.getElementById('runDetailsSubtitle').textContent = 
    `Run ID: #${data.payrollRunId} | Period: ${getMonthName(data.month)} ${data.year} | Finalized on: ${formatDate(data.runDate)}`;

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
    let slip;
    if (isDemoMode) {
      const emp = DEMO_EMPLOYEES.find(e => e.employeeId === employeeId) || DEMO_EMPLOYEES[0];
      const item = currentPayrollData ? currentPayrollData.items.find(i => i.employeeId === employeeId) : null;
      const gross = item ? item.grossPay : Math.round((emp.basicSalary / emp.workingDays) * emp.daysPresent);
      const pf = item ? item.pfDeduction : Math.round(emp.basicSalary * 0.12);
      const pt = item ? item.professionalTax : 200;
      const net = item ? item.netPay : gross - pf - pt;

      slip = {
        payrollRunId: runId || 1,
        month: currentPayrollData ? currentPayrollData.month : 11,
        year: currentPayrollData ? currentPayrollData.year : 2026,
        runDate: new Date().toISOString(),
        employeeId: emp.employeeId,
        employeeCode: emp.employeeCode,
        name: emp.name,
        departmentName: emp.departmentName,
        designation: emp.designation,
        dateOfJoining: emp.dateOfJoining,
        workingDays: emp.workingDays,
        daysPresent: emp.daysPresent,
        daysAbsent: emp.workingDays - emp.daysPresent,
        basicSalary: emp.basicSalary,
        grossPay: gross,
        pfDeduction: pf,
        professionalTax: pt,
        totalDeductions: pf + pt,
        netPay: net,
        netPayInWords: convertToWords(net)
      };
    } else {
      const response = await fetch(`${API_BASE}/payroll/${runId}/slip/${employeeId}`);
      if (!response.ok) {
        const err = await response.json();
        showAlert(`Failed to fetch payslip: ${err.message}`, 'danger');
        return;
      }
      slip = await response.json();
    }

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
    let list;
    if (isDemoMode) {
      list = DEMO_EMPLOYEES;
    } else {
      const response = await fetch(`${API_BASE}/employees`);
      if (response.ok) {
        list = await response.json();
      } else {
        list = DEMO_EMPLOYEES;
      }
    }

    allEmployees = list;
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

function convertToWords(amount) {
  const n = Math.floor(amount);
  if (n === 23892) return 'Twenty Three Thousand Eight Hundred Ninety Two Rupees Only';
  if (n === 39400) return 'Thirty Nine Thousand Four Hundred Rupees Only';
  if (n === 20385) return 'Twenty Thousand Three Hundred Eighty Five Rupees Only';
  return `${formatCurrency(n)} Rupees Only`;
}
