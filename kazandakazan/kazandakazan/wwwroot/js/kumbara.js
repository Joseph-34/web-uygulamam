(function () {
  var popTimer;
  var vaultBusy = false;
  var LS_TOSS = "kk_toss_amount";
  var LS_RES = "kk_reserve_amount";

  function getCsrf() {
    var m = document.querySelector('meta[name="csrf-token"]');
    return m ? m.getAttribute("content") || "" : "";
  }

  function fmtMoney(n) {
    if (n == null || Number.isNaN(n)) return "—";
    return new Intl.NumberFormat("tr-TR", {
      style: "currency",
      currency: "TRY",
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(n);
  }

  function showToast(message, variant) {
    var el = document.getElementById("app-toast");
    if (!el) {
      if (variant === "error") alert(message);
      return;
    }
    el.textContent = message;
    el.className = "app-toast app-toast--show" + (variant === "error" ? " app-toast--error" : "");
    clearTimeout(el._t);
    el._t = setTimeout(function () {
      el.classList.remove("app-toast--show");
    }, 4200);
  }

  function setLiquidFill(pct) {
    var svg = document.querySelector("[data-fund-svg]");
    var el = document.getElementById("fund-liquid-fill");
    if (!svg || !el) return;
    var maxH = parseFloat(svg.getAttribute("data-fill-max") || "196");
    var bottom = parseFloat(svg.getAttribute("data-fill-bottom") || "284");
    var p = Math.max(0, Math.min(100, Number(pct) || 0));
    var H = (maxH * p) / 100;
    el.setAttribute("y", (bottom - H).toFixed(2));
    el.setAttribute("height", H.toFixed(2));

    var wrap = document.querySelector(".fund-vault-wrap");
    var hit = document.querySelector(".fund-vault-hit");
    if (wrap) wrap.classList.toggle("fund-vault-wrap--hot", p >= 99);
    if (hit) hit.classList.toggle("fund-vault-hit--hot", p >= 99);
  }

  function applyPublicPotState(dto) {
    if (!dto) return;

    var cur = document.querySelector("[data-pot-current]");
    var tgt = document.querySelector("[data-pot-target]");
    var tickets = document.querySelector("[data-pot-tickets]");
    var pctBig = document.querySelector("[data-pot-percent-display]");
    var tgtLine = document.querySelector("[data-pot-target-line]");
    var potIdEls = document.querySelectorAll("[data-pot-id-input]");

    setLiquidFill(dto.fillPercent || 0);

    if (cur) cur.textContent = fmtMoney(dto.currentBalance);
    if (tgt) tgt.textContent = fmtMoney(dto.targetAmount);
    if (tickets) tickets.textContent = (dto.totalTickets ?? 0).toLocaleString("tr-TR");

    if (pctBig && dto.fillPercent != null) {
      pctBig.textContent =
        dto.fillPercent.toLocaleString("tr-TR", { maximumFractionDigits: 1 }) + "%";
    }
    if (tgtLine && dto.targetAmount != null) {
      tgtLine.textContent =
        "Hedef " +
        dto.targetAmount.toLocaleString("tr-TR", { maximumFractionDigits: 0 }) +
        " ₺";
    }

    var vault = document.querySelector("[data-vault]");
    if (vault && dto.currentBalance != null) {
      vault.setAttribute("data-current-balance", String(dto.currentBalance));
    }

    if (dto.potId != null) {
      potIdEls.forEach(function (x) {
        x.value = dto.potId;
      });
      var panel = document.querySelector("[data-pot-id]");
      if (panel) panel.setAttribute("data-pot-id", String(dto.potId));
    }
  }

  function showVaultPop(vault, balance) {
    var pop = vault.querySelector("[data-vault-pop]");
    if (!pop) return;
    pop.textContent = fmtMoney(balance);
    pop.hidden = false;
    if (popTimer) clearTimeout(popTimer);
    popTimer = setTimeout(function () {
      pop.hidden = true;
    }, 2200);
  }

  async function postPot(url, fields) {
    var fd = new FormData();
    var t = getCsrf();
    fd.append("__RequestVerificationToken", t);
    Object.keys(fields).forEach(function (k) {
      fd.append(k, fields[k]);
    });
    var res = await fetch(url, {
      method: "POST",
      body: fd,
      credentials: "same-origin",
      headers: t ? { RequestVerificationToken: t } : {},
    });
    var data = await res.json().catch(function () {
      return null;
    });
    if (!res.ok || !data) {
      showToast("Yenileyip deneyin.", "error");
      return null;
    }
    return data;
  }

  function handleWinnerResponse(data) {
    if (data && data.winnerUserName) {
      showWinnerCelebration(data.winnerUserName);
    }
  }

  function getSupportTel() {
    var b = document.body.getAttribute("data-support-tel") || "";
    return b.trim();
  }

  function wireWinnerOverlay() {
    var overlay = document.getElementById("winner-overlay");
    var link = document.getElementById("winner-care-link");
    if (!overlay || !link) return;

    var tel = getSupportTel();
    if (tel) {
      var digits = tel.replace(/\s/g, "");
      link.href = "tel:" + digits;
      link.removeAttribute("aria-disabled");
    } else {
      link.href = "#";
      link.addEventListener("click", function (e) {
        e.preventDefault();
        showToast("Destek numarası yok.", "error");
      });
    }

    overlay.querySelectorAll("[data-close-winner]").forEach(function (el) {
      el.addEventListener("click", function () {
        overlay.hidden = true;
      });
    });
  }

  function burstConfetti(canvas) {
    if (!canvas || !canvas.getContext) return;
    var ctx = canvas.getContext("2d");
    var dpr = window.devicePixelRatio || 1;
    var w = (canvas.width = canvas.offsetWidth * dpr);
    var h = (canvas.height = canvas.offsetHeight * dpr);
    ctx.scale(dpr, dpr);
    var particles = [];
    var colors = ["#fbbf24", "#f472b6", "#22c55e", "#38bdf8", "#a78bfa", "#fb7185"];
    for (var i = 0; i < 90; i++) {
      particles.push({
        x: canvas.offsetWidth / 2,
        y: canvas.offsetHeight * 0.35,
        vx: (Math.random() - 0.5) * 14,
        vy: Math.random() * -12 - 4,
        g: 0.35 + Math.random() * 0.2,
        r: 3 + Math.random() * 5,
        c: colors[(Math.random() * colors.length) | 0],
        life: 80 + Math.random() * 40,
      });
    }
    var frame = 0;
    function tick() {
      frame++;
      ctx.clearRect(0, 0, canvas.offsetWidth, canvas.offsetHeight);
      particles.forEach(function (p) {
        if (frame > p.life) return;
        p.vy += p.g;
        p.x += p.vx;
        p.y += p.vy;
        ctx.fillStyle = p.c;
        ctx.globalAlpha = Math.max(0, 1 - frame / p.life);
        ctx.beginPath();
        ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
        ctx.fill();
      });
      ctx.globalAlpha = 1;
      if (frame < 95) requestAnimationFrame(tick);
    }
    tick();
  }

  function showWinnerCelebration(userName) {
    var overlay = document.getElementById("winner-overlay");
    var nameEl = document.getElementById("winner-modal-name");
    var canvas = document.getElementById("winner-confetti");
    if (!overlay || !nameEl) return;
    nameEl.textContent = userName;
    overlay.hidden = false;
    if (canvas) {
      burstConfetti(canvas);
    }
  }

  window.kkShowWinnerCelebration = showWinnerCelebration;

  async function refreshPersonal() {
    var auth = document.body.getAttribute("data-authenticated") === "true";
    if (!auth) return;
    try {
      var res = await fetch("/Home/PotStateJson", {
        credentials: "same-origin",
        headers: { Accept: "application/json" },
      });
      if (!res.ok) return;
      var dto = await res.json();
      if (!dto) return;

      var w = document.querySelector("[data-user-wallet]");
      var ut = document.querySelector("[data-user-tickets]");
      var ch = document.querySelector("[data-user-chance]");

      if (w) w.textContent = fmtMoney(dto.userWallet);
      if (ut) ut.textContent = (dto.userTicketCount ?? 0).toLocaleString("tr-TR");
      if (ch) {
        var p = dto.userWinChancePercent;
        ch.textContent =
          p == null ? "—" : p.toLocaleString("tr-TR", { maximumFractionDigits: 2 }) + "%";
      }

      applyPublicPotState(dto);
    } catch {
      /* ignore */
    }
  }

  window.applyPublicPotState = applyPublicPotState;
  window.refreshPersonalPotState = refreshPersonal;

  function getTossAmount() {
    var toss = document.querySelector("[data-toss-select]");
    if (!toss || !toss.value) return 0;
    return parseFloat(toss.value);
  }

  function potId() {
    var h = document.querySelector("[data-pot-id-input]");
    var panel = document.querySelector("[data-pot-id]");
    if (h && h.value) return parseInt(h.value, 10);
    if (panel) return parseInt(panel.getAttribute("data-pot-id") || "0", 10);
    return 0;
  }

  async function contributeSelected() {
    var amt = getTossAmount();
    if (!amt || amt <= 0) {
      showToast("Tutar seçin.", "error");
      return;
    }
    var data = await postPot("/Home/ContributeAjax", {
      potId: String(potId()),
      amount: String(amt),
    });
    if (!data) return;
    if (data.success && data.pot) applyPublicPotState(data.pot);
    await refreshPersonal();
    showToast(data.message || (data.success ? "Tamam." : "Olmadı."), data.success ? "ok" : "error");
    if (data.success) {
      handleWinnerResponse(data);
      var vault = document.querySelector("[data-vault]");
      var bal = data.pot && data.pot.currentBalance != null ? data.pot.currentBalance : null;
      if (vault && bal != null) showVaultPop(vault, bal);
    }
  }

  function bindVaultClick() {
    var vault = document.querySelector("[data-vault]");
    if (!vault) return;
    vault.addEventListener("click", async function () {
      vault.classList.remove("fund-vault-hit--shake");
      void vault.offsetWidth;
      vault.classList.add("fund-vault-hit--shake");

      var interactive = vault.getAttribute("data-vault-interactive") === "true";
      if (interactive && !vaultBusy) {
        vaultBusy = true;
        try {
          await contributeSelected();
        } finally {
          vaultBusy = false;
        }
        return;
      }

      var raw = vault.getAttribute("data-current-balance");
      var bal = raw != null ? parseFloat(raw) : NaN;
      if (!Number.isNaN(bal)) showVaultPop(vault, bal);
    });
  }

  function restoreSelects() {
    var toss = document.querySelector("[data-toss-select]");
    var res = document.querySelector("[data-reserve-select]");
    try {
      function hasOption(sel, val) {
        return Array.prototype.some.call(sel.options, function (o) {
          return o.value === val;
        });
      }
      if (toss && localStorage.getItem(LS_TOSS)) {
        var v = localStorage.getItem(LS_TOSS);
        if (hasOption(toss, v)) toss.value = v;
      }
      if (res && localStorage.getItem(LS_RES)) {
        var r = localStorage.getItem(LS_RES);
        if (hasOption(res, r)) res.value = r;
      }
    } catch {
      /* private mode */
    }
    if (toss)
      toss.addEventListener("change", function () {
        try {
          localStorage.setItem(LS_TOSS, toss.value);
        } catch {
          /* */
        }
      });
    if (res)
      res.addEventListener("change", function () {
        try {
          localStorage.setItem(LS_RES, res.value);
        } catch {
          /* */
        }
      });
  }

  window.kkInitPotPage = function () {
    wireWinnerOverlay();
    bindVaultClick();
    restoreSelects();

    document.querySelectorAll("[data-demo-amount]").forEach(function (b) {
      if (b.getAttribute("data-demo-bound") === "1") return;
      b.setAttribute("data-demo-bound", "1");
      b.addEventListener("click", async function () {
        var amt = b.getAttribute("data-demo-amount");
        var data = await postPot("/Home/DemoWalletAjax", { amount: amt });
        if (!data) return;
        if (data.success && data.pot) applyPublicPotState(data.pot);
        await refreshPersonal();
        showToast(data.message || (data.success ? "Tamam." : "Olmadı."), data.success ? "ok" : "error");
      });
    });

    var bankForm = document.querySelector("[data-bank-deposit-form]");
    if (bankForm) {
      bankForm.addEventListener("submit", async function (e) {
        e.preventDefault();
        var inp = bankForm.querySelector("[data-bank-amount]");
        var sel = bankForm.querySelector("[data-bank-label]");
        var raw = inp && inp.value ? inp.value.trim().replace(",", ".") : "";
        var amount = parseFloat(raw);
        if (!Number.isFinite(amount) || amount < 5) {
          showToast("En az 5 ₺.", "error");
          return;
        }
        var units = amount / 5;
        if (Math.abs(units - Math.round(units)) > 1e-6) {
          showToast("5 ₺ katı.", "error");
          return;
        }
        var rounded = Math.round(units) * 5;
        if (rounded > 500000) {
          showToast("Max 500.000 ₺.", "error");
          return;
        }
        var bankLabel = sel && sel.value ? sel.value : "";
        var data = await postPot("/Home/BankDepositSimulateAjax", {
          amount: String(rounded),
          bankAccountLabel: bankLabel,
        });
        if (!data) return;
        if (data.success && data.pot) applyPublicPotState(data.pot);
        await refreshPersonal();
        showToast(data.message || (data.success ? "Tamam." : "Olmadı."), data.success ? "ok" : "error");
        if (data.success && inp) inp.value = "";
      });
    }

    var panel = document.querySelector("[data-pot-id]");
    if (!panel) return;

    var btnOne = document.querySelector('[data-action="contribute-one"]');
    var btnAll = document.querySelector('[data-action="contribute-all"]');
    var resSel = document.querySelector("[data-reserve-select]");

    if (btnOne) {
      btnOne.addEventListener("click", async function () {
        await contributeSelected();
      });
    }

    if (btnAll) {
      btnAll.addEventListener("click", async function () {
        var res = parseFloat(resSel && resSel.value ? resSel.value : "0");
        var data = await postPot("/Home/ContributeAllAjax", {
          potId: String(potId()),
          reserveAmount: String(res),
        });
        if (!data) return;
        if (data.success && data.pot) applyPublicPotState(data.pot);
        await refreshPersonal();
        showToast(data.message || (data.success ? "Tamam." : "Olmadı."), data.success ? "ok" : "error");
        if (data.success) {
          handleWinnerResponse(data);
          var vault = document.querySelector("[data-vault]");
          var bal = data.pot && data.pot.currentBalance != null ? data.pot.currentBalance : null;
          if (vault && bal != null) showVaultPop(vault, bal);
        }
      });
    }
  };

  document.addEventListener("DOMContentLoaded", function () {
    if (typeof signalR === "undefined") return;

    var conn = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/pot")
      .withAutomaticReconnect()
      .build();

    conn.on("potUpdated", function (dto) {
      applyPublicPotState(dto);
      refreshPersonal();
    });

    conn.on("winnerAnnounced", function (payload) {
      if (payload && payload.userName) {
        showWinnerCelebration(payload.userName);
      }
    });

    conn.start().catch(function () {
      /* offline */
    });
  });
})();
