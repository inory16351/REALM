/* Only the public-discard buttons can open a preview. No hidden cards are read. */
(() => {
  const preview = document.createElement("aside");
  preview.id = "grave-card-preview";
  preview.className = "grave-card-preview hidden";
  preview.setAttribute("role", "tooltip");
  document.body.append(preview);
  let anchor = null;
  let pinned = false;
  let hideTimer;
  let catalog = {};
  fetch("assets/role-card-art-v3/catalog.json").then(response => response.ok ? response.json() : {}).then(data => {
    catalog = data;
    if (anchor) paint();
  }).catch(() => {});

  function close() {
    clearTimeout(hideTimer);
    anchor?.removeAttribute("aria-describedby");
    anchor = null;
    pinned = false;
    preview.classList.add("hidden");
  }
  function position() {
    if (!anchor?.isConnected) return close();
    const rect = anchor.getBoundingClientRect();
    const width = preview.offsetWidth, height = preview.offsetHeight, gap = 12;
    let left = rect.right + gap;
    if (left + width > window.innerWidth - gap) left = rect.left - width - gap;
    left = Math.max(gap, Math.min(left, window.innerWidth - width - gap));
    const top = Math.max(gap, Math.min(rect.top, window.innerHeight - height - gap));
    preview.style.left = `${left}px`;
    preview.style.top = `${top}px`;
  }
  function paint() {
    const type = anchor.dataset.graveType;
    const role = ROLE_DATA[type];
    if (!role) return close();
    const suffix = Number(anchor.dataset.graveCard.split("-").at(-1));
    const number = Number.isInteger(suffix) && suffix >= 0 && suffix < 12 ? suffix + 1 : 1;
    const detail = catalog[type]?.rule || ruleText(type);
    preview.innerHTML = `<div class="grave-preview-art"><img src="assets/role-card-art-v3/${type}-${String(number).padStart(2, "0")}.jpg" alt="${escapeHtml(role.name)} 카드 일러스트" /><div class="grave-preview-heading"><span class="grave-preview-score">${signed(role.score)}<small>기본 점수</small></span><strong>${escapeHtml(role.name)}</strong></div></div><div class="grave-preview-copy"><p class="grave-preview-owner">${escapeHtml(anchor.dataset.graveOwner)} · 공개 무덤</p><div class="grave-preview-bonus">${role.bonus ? `성공 시 +${role.bonus}점` : "단조 능력으로 추가 점수"}</div><p>${escapeHtml(detail)}</p><small>클릭으로 고정 · Esc 또는 바깥쪽 클릭으로 닫기</small></div>`;
    preview.classList.remove("hidden");
    anchor.setAttribute("aria-describedby", preview.id);
    position();
  }
  function open(button, pin = false) {
    clearTimeout(hideTimer);
    if (anchor !== button) anchor?.removeAttribute("aria-describedby");
    anchor = button;
    pinned = pin;
    paint();
  }
  function scheduleClose() {
    if (!pinned) hideTimer = setTimeout(close, 180);
  }
  document.addEventListener("pointerover", event => {
    if (event.pointerType === "touch") return;
    if (preview.contains(event.target)) { clearTimeout(hideTimer); return; }
    const button = event.target.closest("[data-grave-card]");
    if (button && button !== anchor) open(button);
    else if (button) clearTimeout(hideTimer);
  });
  document.addEventListener("pointerout", event => {
    if (!anchor || pinned) return;
    const to = event.relatedTarget;
    if (to && (preview.contains(to) || anchor.contains(to))) return;
    if (preview.contains(event.target) || anchor.contains(event.target)) scheduleClose();
  });
  document.addEventListener("focusin", event => {
    const button = event.target.closest("[data-grave-card]");
    if (button) open(button);
    else if (!preview.contains(event.target)) close();
  });
  document.addEventListener("focusout", event => {
    if (anchor && !pinned && !preview.contains(event.relatedTarget) && event.relatedTarget !== anchor) scheduleClose();
  });
  document.addEventListener("click", event => {
    const button = event.target.closest("[data-grave-card]");
    if (button) {
      if (anchor === button && pinned) close();
      else open(button, true);
    } else if (!preview.contains(event.target)) close();
  });
  document.addEventListener("keydown", event => { if (event.key === "Escape") close(); });
  window.addEventListener("resize", () => { if (anchor) position(); });
  document.addEventListener("scroll", () => { if (anchor) position(); }, true);
  // A rerender or a closed grave dialog must not leave a detached preview behind.
  new MutationObserver(() => { if (anchor && !anchor.isConnected) close(); }).observe(document.querySelector(".app-shell"), { childList: true, subtree: true });
  new MutationObserver(() => { if (anchor && !anchor.isConnected) close(); }).observe(document.body, { childList: true, subtree: true });
})();
