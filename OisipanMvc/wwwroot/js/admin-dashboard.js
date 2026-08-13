// admin-dashboard.js - plain JS to fetch dashboard stats and render a polished SVG chart
(function(){
  async function fetchStats(){
    // Always fetch live data from the API. If network fails, fall back to server-rendered payload.
    const base = window.apiBaseUrl || '';
    const url = base ? `${base.replace(/\/$/, '')}/api/dashboard/stats` : '/api/dashboard/stats';

    try{
      const resp = await fetch(url, { mode: 'cors' });
      if(!resp.ok) throw new Error('Network response not ok');
      return await resp.json();
    }catch(e){
      console.error('Failed fetching dashboard stats, falling back to server payload', e);
      if(window.dashboardStats) return window.dashboardStats;
      return null;
    }
  }

  function formatCurrency(v){return (Number(v)||0).toLocaleString('vi-VN') + 'đ';}

  function normalizeDashboardStats(data){
    if(!data) return {
      todayRevenue: 0,
      monthRevenue: 0,
      pendingOrders: 0,
      confirmedOrders: 0,
      lowStockProducts: 0,
      outOfStockProducts: 0,
      newCustomersThisMonth: 0,
      totalCustomers: 0,
      recentOrders: [],
      dailyRevenues: []
    };

    return {
      todayRevenue: data.todayRevenue ?? data.TodayRevenue ?? 0,
      monthRevenue: data.monthRevenue ?? data.MonthRevenue ?? 0,
      pendingOrders: data.pendingOrders ?? data.PendingOrders ?? 0,
      confirmedOrders: data.confirmedOrders ?? data.ConfirmedOrders ?? 0,
      lowStockProducts: data.lowStockProducts ?? data.LowStockProducts ?? 0,
      outOfStockProducts: data.outOfStockProducts ?? data.OutOfStockProducts ?? 0,
      newCustomersThisMonth: data.newCustomersThisMonth ?? data.NewCustomersThisMonth ?? 0,
      totalCustomers: data.totalCustomers ?? data.TotalCustomers ?? 0,
      recentOrders: data.recentOrders ?? data.RecentOrders ?? [],
      dailyRevenues: data.dailyRevenues ?? data.DailyRevenues ?? []
    };
  }

  function createSvgElement(tag, attrs = {}){
    const el = document.createElementNS('http://www.w3.org/2000/svg', tag);
    Object.entries(attrs).forEach(([key, value]) => el.setAttribute(key, value));
    return el;
  }

  function renderMetrics(data){
    const todayEl = document.querySelector('.metric-card.glow-green strong');
    if(todayEl) todayEl.textContent = formatCurrency(data.todayRevenue);
    const pendingEl = document.querySelector('.metric-card.glow-gold strong');
    if(pendingEl) pendingEl.textContent = (data.pendingOrders || 0);
    const lowStockEl = document.querySelector('.metric-card.glow-red strong');
    if(lowStockEl) lowStockEl.textContent = (data.lowStockProducts || 0);
    const customersEl = document.querySelector('.metric-card.glow-blue strong');
    if(customersEl) customersEl.textContent = (data.totalCustomers || 0);

    const monthRevenueEl = document.querySelector('.chart-meta-right strong');
    if(monthRevenueEl) monthRevenueEl.textContent = formatCurrency(data.monthRevenue);
  }

  function renderTasks(data){
    const labels = document.querySelectorAll('.task-list label');
    if(labels && labels.length){
      labels[0].innerHTML = '<input type="checkbox"> Xác nhận ' + (data.pendingOrders || 0) + ' đơn chờ';
      labels[1].innerHTML = '<input type="checkbox"> Kiểm tra ' + (data.lowStockProducts || 0) + ' sản phẩm sắp hết';
    }
  }

  function renderOrders(data){
    const tbody = document.querySelector('.admin-table tbody');
    if(!tbody) return;
    tbody.innerHTML = '';
    (data.recentOrders||[]).forEach(order =>{
      const orderId = order.orderId ?? order.OrderId ?? '';
      const orderCode = order.orderCode ?? order.OrderCode ?? '';
      const customerName = order.customerName ?? order.CustomerName ?? '';
      const totalAmount = order.totalAmount ?? order.TotalAmount ?? 0;
      const paymentMethod = order.paymentMethod ?? order.PaymentMethod ?? '';
      const status = order.status ?? order.Status ?? '';
      const estimatedDelivery = order.estimatedDelivery ?? order.EstimatedDelivery ?? '';

      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td><a href="/Admin/Orders/Detail/${orderId}" style="text-decoration:none;color:inherit;font-weight:500">#${orderCode}</a></td>
        <td>${customerName}</td>
        <td>${formatCurrency(totalAmount)}</td>
        <td>${paymentMethod}</td>
        <td><span class="status">${status}</span></td>
        <td>${estimatedDelivery}</td>
      `;
      tbody.appendChild(tr);
    });
  }

  function renderChart(data){
    const chartEl = document.getElementById('monthRevenueChart');
    const emptyEl = document.getElementById('growthChartEmpty');
    const frameEl = chartEl?.closest('.chart-frame');
    const summaryEl = document.getElementById('chartSummary');
    const trendEl = document.getElementById('growthTrendLabel');
    if(!chartEl) return;

    const daily = data.dailyRevenues || [];
    if(!daily.length){
      frameEl?.classList.add('has-empty');
      if(emptyEl) emptyEl.style.display = 'flex';
      chartEl.innerHTML = '';
      return;
    }

    frameEl?.classList.remove('has-empty');
    const values = daily.map(item => Number(item.revenue || 0));
    const labels = daily.map(item => item.date || '');
    const width = 640;
    const height = 280;
    const padding = { top: 24, right: 24, bottom: 44, left: 48 };
    const chartWidth = width - padding.left - padding.right;
    const chartHeight = height - padding.top - padding.bottom;
    const maxValue = Math.max(...values, 1);
    const minValue = 0;
    const stepX = values.length > 1 ? chartWidth / (values.length - 1) : chartWidth;

    const points = values.map((value, index) => ({
      x: padding.left + index * stepX,
      y: padding.top + chartHeight - ((value - minValue) / (maxValue - minValue || 1)) * chartHeight
    }));

    const baseY = padding.top + chartHeight;
    const areaPath = `M ${points[0].x} ${baseY} L ${points.map(point => `${point.x} ${point.y}`).join(' L ')} L ${points[points.length - 1].x} ${baseY} Z`;
    const linePath = points.map((point, index) => `${index === 0 ? 'M' : 'L'} ${point.x.toFixed(2)} ${point.y.toFixed(2)}`).join(' ');

    chartEl.innerHTML = '';

    const defs = createSvgElement('defs');
    const gradient = createSvgElement('linearGradient', { id: 'growthGradient', x1: '0%', y1: '0%', x2: '0%', y2: '100%' });
    gradient.appendChild(createSvgElement('stop', { offset: '0%', 'stop-color': 'rgba(16,185,129,0.38)' }));
    gradient.appendChild(createSvgElement('stop', { offset: '100%', 'stop-color': 'rgba(16,185,129,0.03)' }));
    defs.appendChild(gradient);
    chartEl.appendChild(defs);

    for (let i = 0; i <= 3; i += 1) {
      const y = padding.top + (chartHeight / 3) * i;
      chartEl.appendChild(createSvgElement('line', { x1: padding.left, y1: y, x2: width - padding.right, y2: y, class: 'grid' }));
      const label = createSvgElement('text', { x: 8, y: y + 4, class: 'label' });
      label.textContent = formatCurrency(maxValue * (1 - i / 3));
      chartEl.appendChild(label);
    }

    chartEl.appendChild(createSvgElement('line', { x1: padding.left, y1: baseY, x2: width - padding.right, y2: baseY, class: 'axis' }));
    chartEl.appendChild(createSvgElement('line', { x1: padding.left, y1: padding.top, x2: padding.left, y2: baseY, class: 'axis' }));
    chartEl.appendChild(createSvgElement('path', { d: areaPath, class: 'area' }));
    chartEl.appendChild(createSvgElement('path', { d: linePath, class: 'line' }));

    points.forEach((point, index) => {
      const circle = createSvgElement('circle', { cx: point.x, cy: point.y, r: 5, class: 'point' });
      circle.setAttribute('tabindex', '0');
      circle.setAttribute('aria-label', `${labels[index]}: ${formatCurrency(values[index])}`);
      circle.addEventListener('mouseenter', () => {
        if(summaryEl) summaryEl.textContent = `${labels[index]} • ${formatCurrency(values[index])}`;
      });
      circle.addEventListener('focus', () => {
        if(summaryEl) summaryEl.textContent = `${labels[index]} • ${formatCurrency(values[index])}`;
      });
      chartEl.appendChild(circle);

      if(index % Math.max(1, Math.floor(points.length / 5)) === 0 || index === points.length - 1){
        const label = createSvgElement('text', { x: point.x, y: height - 12, class: 'label' });
        label.setAttribute('text-anchor', 'middle');
        label.textContent = labels[index];
        chartEl.appendChild(label);
      }
    });

    const totalRevenue = values.reduce((sum, value) => sum + value, 0);
    if(summaryEl) summaryEl.textContent = `Tổng ${formatCurrency(totalRevenue)}`;
    if(trendEl){
      const firstValue = values[0] || 0;
      const lastValue = values[values.length - 1] || 0;
      const growth = firstValue ? ((lastValue - firstValue) / firstValue) * 100 : 0;
      trendEl.textContent = `${growth >= 0 ? '+' : ''}${growth.toFixed(1)}% so với đầu tháng`;
    }
  }

  async function init(){
    const data = await fetchStats();
    if(!data) return;
    const stats = normalizeDashboardStats(data);
    renderMetrics(stats);
    renderTasks(stats);
    renderOrders(stats);
    renderChart(stats);
  }

  if(document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();