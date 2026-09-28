const ROLE_DATA = {
  king:{name:"왕",score:-1,bonus:9,rule:"전체 무덤의 왕 카드가 9장 이상."}, noble:{name:"귀족",score:1,bonus:7,rule:"전원이 막라에 비공개로 버린 귀족 카드가 정확히 3장."}, assassin:{name:"암살자",score:0,bonus:10,rule:"서로 다른 두 상대의 직업을 모두 맞힘."}, beggar:{name:"거지",score:-1,bonus:10,rule:"전원 손패 합계가 인원수 × 2보다 적음."}, slave:{name:"노예",score:0,bonus:7,rule:"미공개 상대의 직업을 맞히고 그 직업이 성공."}, jester:{name:"광대",score:-1,bonus:8,rule:"암살자에게 광대가 아닌 다른 직업으로 지목당함."}, priest:{name:"성직자",score:1,bonus:8,rule:"전원이 성직자 카드를 1장 이상 보유."}, knight:{name:"기사",score:1,bonus:7,rule:"기사가 전체 무덤에서 최저 버림 종류 중 하나."}, bard:{name:"음유시인",score:0,bonus:9,rule:"손패가 음유시인뿐이고 막라에 1장 이상 버림."}, hunter:{name:"냥꾼",score:0,bonus:8,rule:"상대 손패의 카드 종류를 맞힘."}, commoner:{name:"평민",score:1,bonus:7,rule:"평민이 전원 손패에 가장 많이 남은 종류 중 하나."}, merchant:{name:"상인",score:1,bonus:6,rule:"전원 손패 합계가 인원수 × 2보다 많음."}, blacksmith:{name:"대장장이",score:0,bonus:0,rule:"막라 더미 3장 단조. 절댓값 2배, 최대 12점."}, thief:{name:"도적",score:0,bonus:9,rule:"강탈한 두 카드가 같은 종류."}, mercenary:{name:"용병",score:0,bonus:9,rule:"공개 무덤과 최대 2장 교환 후 지정한 4장이 모두 같은 종류."}, seer:{name:"점술가",score:0,bonus:7,rule:"손패 2장 이상, 모두 같은 종류."}, alchemist:{name:"연금술사",score:-1,bonus:8,rule:"손에 -1·0·+1점 카드가 각각 1장 이상."}, librarian:{name:"사서",score:1,bonus:10,rule:"선택된 직업 카드 5종을 각각 1장 이상 보유."}, mage:{name:"마법사",score:0,bonus:8,rule:"손패 3장 이상, 카드 점수 합이 정확히 0."}, farmer:{name:"농민",score:1,bonus:7,rule:"손패 5장 이상, 농민 카드 3장 이상."}, courtesan:{name:"매춘부",score:0,bonus:8,rule:"지목한 미공개 상대 직업 카드가 내 손에 2장 이상."}, pope:{name:"교황",score:1,bonus:8,rule:"무덤 교황 6장 이상 + 내 손에 교황 1장 이상."}, barbarian:{name:"바바리안",score:-1,bonus:8,rule:"1~3라 공개 버림 5장 이상이며 전원 최다."}, chancellor:{name:"재상",score:1,bonus:7,rule:"전원 손패에서 가장 많은 카드 종류를 예측."}, queen:{name:"왕비",score:1,bonus:7,rule:"왕비 2장 이상, 왕비 보유 수 단독 최다."}
};
const ROLE_KEYS = Object.keys(ROLE_DATA);
const CARD_RULES = {
  king:"왕 9장 이상 버리기. 9장 이하 투입 시 손패에 왕 없음.", noble:"전원이 4라운드에 비공개로 버린 귀족이 정확히 3장.", assassin:"서로 다른 두 상대의 직업을 모두 맞히기.", beggar:"전원 손패 합계가 기준보다 적음.", slave:"미공개 상대 직업을 맞히고 그 직업도 성공.", jester:"암살자에게 광대가 아닌 다른 직업으로 지목되기.", priest:"전원 손패에 성직자 1장 이상.", knight:"기사의 전체 무덤 수가 최저.", bard:"손패가 음유시인뿐이고 4라운드에 1장 이상 버림.", hunter:"상대 손패에 있는 카드 종류를 맞히기.", commoner:"평민이 전원 손패의 최다 종류.", merchant:"전원 손패 합계가 기준보다 많음.", blacksmith:"4라운드 비공개 더미에서 3장 단조.", thief:"강탈한 두 카드의 종류가 같음.", mercenary:"공개 무덤과 최대 2장 교환 후 목표 4장이 동일.", seer:"손패 2장 이상이 모두 같은 종류.", alchemist:"-1·0·+1점 카드를 각각 1장 이상.", librarian:"선택된 카드 종류를 최소 5종 보유.", mage:"손패 3장 이상, 카드 점수 합 0.", farmer:"손패 5장 이상, 농민 3장 이상.", courtesan:"내 손에 지목 상대 직업 카드 2장 이상.", pope:"전체 무덤 교황 6장 이상 + 손패 1장 이상.", barbarian:"공개 버림 5장 이상이며 전원 최다.", chancellor:"전원 손패의 최다 카드 종류를 맞히기.", queen:"왕비 2장 이상, 단독 최다 보유."
};
const $ = (selector) => document.querySelector(selector);
let state = null;
let socket = null;
let chosenCards = [];
let localSelected = [];
let diceRoll = null;
let diceTimer = null;
let discussionTimer = null;
const signed = (score) => score > 0 ? `+${score}` : `${score}`;
const typeName = (key) => ROLE_DATA[key]?.name || key;
const escapeHtml = (value) => String(value).replace(/[&<>"']/g, (char) => ({"&":"&amp;","<":"&lt;",">":"&gt;","\"":"&quot;","'":"&#039;"}[char]));
const pos = (key) => { const index = ROLE_KEYS.indexOf(key); return `${(index % 5) * 25}% ${Math.floor(index / 5) * 25}%`; };
const ruleText = (key) => { if (!state) return CARD_RULES[key] || ROLE_DATA[key].rule; if(key === "beggar") return `전원 손패 합계가 ${state.playerTarget * 2}장보다 적음.`; if(key === "merchant") return `전원 손패 합계가 ${state.playerTarget * 2}장보다 많음.`; return CARD_RULES[key] || ROLE_DATA[key].rule; };

function toast(message) { const el = $("#toast"); el.textContent = message; el.classList.remove("hidden"); clearTimeout(toast.timer); toast.timer = setTimeout(() => el.classList.add("hidden"), 3600); }
function randomSet(count) { for(let attempt=0; attempt<500; attempt++) { const set=[...ROLE_KEYS].sort(()=>Math.random()-.5).slice(0,count); if(validSet(set,count)==="") return set; } return ["king","noble","assassin","beggar","priest","knight","bard","commoner","merchant","seer"].slice(0,count); }
function selectionScore(keys) { return keys.reduce((sum,key)=>sum+ROLE_DATA[key].score,0); }
function validSet(keys,count) { if(keys.length !== count) return `직업을 정확히 ${count}개 골라야 합니다.`; const scores=keys.map(key=>ROLE_DATA[key].score); const limit=Math.ceil(count*.4), total=selectionScore(keys); if(scores.filter(v=>v<0).length<1||scores.filter(v=>v<0).length>limit||scores.filter(v=>v>0).length<1||scores.filter(v=>v>0).length>limit||scores.filter(v=>v===0).length<1) return "점수 균형을 맞춰야 합니다."; if(Math.abs(total)>1) return `카드 자체 점수 합이 0 또는 ±1이어야 합니다. 현재 ${signed(total)}점입니다.`; if(keys.includes("jester")&&!keys.includes("assassin")) return "광대에는 암살자가 필요합니다."; return ""; }
function cardArtSource(card) { const copy = Number(String(card.id || "").split("-").at(-1)); const variant = Number.isInteger(copy) && copy >= 0 && copy < 12 ? copy + 1 : 1; return `assets/role-card-art-v3/${card.type}-${String(variant).padStart(2,"0")}.jpg`; }
function cardHtml(card,{selectable=false,selected=false}={}) { const role=ROLE_DATA[card.type]; return `<button class="card full-art ${selectable?"selectable":""} ${selected?"selected":""}" data-card="${escapeHtml(card.id)}" ${selectable?"":"disabled"}><img class="full-card-illustration" src="${cardArtSource(card)}" alt="" loading="lazy" /><span class="card-score card-score-top ${role.score>0?"plus":role.score<0?"minus":"zero"}">${signed(role.score)}</span><span class="card-name">${role.name}</span><span class="card-rule">${ruleText(card.type)}</span></button>`; }
function cardsHtml(cards, options={}) { return cards.map(card=>cardHtml(card,{...options,selected:options.selected?.includes(card.id)})).join(""); }
function roleOptions(selected) { return ROLE_KEYS.map(key=>{ const role=ROLE_DATA[key]; return `<button type="button" class="role-option ${selected.includes(key)?"selected":""}" data-role="${key}"><span class="role-portrait" style="background-position:${pos(key)}"></span><span class="score ${role.score>0?"plus":role.score<0?"minus":"zero"}">${signed(role.score)}</span><strong>${role.name}</strong><span class="role-rule">${ruleText(key)}</span></button>`; }).join(""); }
function playerOptions({excludeSelf=true,unrevealed=false}={}) { return state.players.filter(player=>(!excludeSelf||player.index!==state.you.index)&&(!unrevealed||!player.role)).map(player=>`<option value="${player.index}">${player.name} (${player.handCount}장)</option>`).join(""); }
function roleSelect() { return state.selected.map(key=>`<option value="${key}">${typeName(key)}</option>`).join(""); }

function renderLanding() {
  $("#room-label").classList.add("hidden"); $("#grave-button").classList.add("hidden"); $("#leave-button").classList.add("hidden");
  $("#game").innerHTML=`<section class="panel intro online-intro"><p class="eyebrow">5~10인 실시간 방</p><h2>같은 방에 모여, 각자 기기에서 플레이하세요.</h2><p class="lead">손패와 비밀 직업은 서버에만 보관됩니다. 방장은 인원수·토론 시간·직업 조합을 정하고, 참가자는 방 코드로 입장합니다.</p><div class="room-choice"><form id="create-room" class="room-form"><h3>새 방 만들기</h3><label>내 이름<input name="name" maxlength="16" placeholder="예: 아린" required /></label><label>시작 인원<select name="target">${[5,6,7,8,9,10].map(n=>`<option value="${n}">${n}명</option>`).join("")}</select></label><label>라운드 종료 후 토론 시간<select name="discussion">${[15,30,45,60,90,120,180].map(seconds=>`<option value="${seconds}" ${seconds===60?"selected":""}>${seconds}초</option>`).join("")}</select></label><p class="help">매 라운드가 끝나면 전원이 이 시간 동안 채팅으로 토론합니다.</p><button>방 만들기</button></form><form id="join-room" class="room-form"><h3>방 참가하기</h3><label>내 이름<input name="name" maxlength="16" placeholder="예: 현우" required /></label><label>방 코드<input name="code" maxlength="8" placeholder="예: A1B2C3D4" required /></label><button class="quiet">방 입장</button></form></div><p class="help">테스트 중에는 시크릿 창 또는 다른 브라우저에서 같은 방 코드를 입력하세요.</p></section>`;
  $("#create-room").onsubmit=async(event)=>{ event.preventDefault(); const form=new FormData(event.currentTarget); await roomRequest("/api/rooms",{name:form.get("name"),playerTarget:Number(form.get("target")),discussionSeconds:Number(form.get("discussion"))}); };
  $("#join-room").onsubmit=async(event)=>{ event.preventDefault(); const form=new FormData(event.currentTarget); await roomRequest(`/api/rooms/${String(form.get("code")).trim().toUpperCase()}/join`,{name:form.get("name")}); };
}
async function roomRequest(path, body) { try { const response=await fetch(path,{method:"POST",headers:{"content-type":"application/json"},body:JSON.stringify(body)}); const data=await response.json(); if(!response.ok) throw new Error(data.error||"방 요청에 실패했습니다."); state=data.state; localSelected=state.selected.length?state.selected:randomSet(state.playerTarget); connect(); render(); } catch(error) { toast(error.message); } }
function connect() { if(!state?.roomCode) return; if(socket) socket.close(); const protocol=location.protocol==="https:"?"wss":"ws"; socket=new WebSocket(`${protocol}://${location.host}/api/rooms/${state.roomCode}/ws`); socket.onmessage=(event)=>{const message=JSON.parse(event.data); if(message.type==="STATE") { const priorRoll=state?.lastRoll?.id; state=message.state; if(!localSelected.length) localSelected=state.selected; if(state.phase==="round"&&state.lastRoll?.round===state.round&&state.lastRoll.id!==priorRoll) playDiceRoll(state.lastRoll); render(); } if(message.type==="ERROR") toast(message.message);}; socket.onclose=()=>{ if(state?.roomCode) setTimeout(()=>{if(!socket||socket.readyState===WebSocket.CLOSED) connect();},1200); }; }
function send(type,payload={}) { if(!socket||socket.readyState!==WebSocket.OPEN) return toast("서버에 다시 연결하는 중입니다."); socket.send(JSON.stringify({type,payload})); }
function playDiceRoll(roll) { diceRoll={...roll,rolling:true}; clearTimeout(diceTimer); renderDiceStage(); diceTimer=setTimeout(()=>{ if(diceRoll?.id===roll.id) { diceRoll.rolling=false; renderDiceStage(); diceTimer=setTimeout(()=>{ if(diceRoll?.id===roll.id) { diceRoll=null; renderDiceStage(); } },1400); } },850); }
function renderDiceStage() { const stage=$("#dice-stage"); if(!stage) return; if(!diceRoll) { stage.classList.add("hidden"); stage.innerHTML=""; return; } const roller=state?.players?.find(player=>player.index===diceRoll.playerIndex)?.name||"플레이어"; const dots=Array.from({length:diceRoll.value},(_,index)=>`<i class="pip pip-${index+1}"></i>`).join("")||'<i class="pip pip-zero"></i>'; stage.classList.remove("hidden"); stage.innerHTML=`<div class="dice-veil"></div><section class="dice-panel"><p class="eyebrow">D6 주사위 · ${diceRoll.round}라운드</p><h2>${escapeHtml(roller)} 플레이어가 주사위를 굴립니다</h2><div class="d6 ${diceRoll.rolling?"rolling":"settled"}"><div class="dice-pips value-${diceRoll.value}">${dots}</div></div><p class="dice-result">${diceRoll.rolling?"주사위를 굴리는 중…":`결과 ${diceRoll.value} · ${diceRoll.value}장 반드시 버리기`}</p><p class="help">면 구성: 0 · 1 · 1 · 2 · 2 · 3</p></section>`; }
function render() { refreshGraveDialog(); if(!state) { renderLanding(); renderDiceStage(); return; } $("#room-label").textContent=`방 코드 ${state.roomCode}`; $("#room-label").classList.remove("hidden"); $("#leave-button").classList.remove("hidden"); $("#grave-button").classList.toggle("hidden",state.phase==="lobby"); if(state.phase!=="discussion") clearInterval(discussionTimer); if(state.phase==="lobby") renderLobby(); else if(state.phase==="round") renderRound(); else if(state.phase==="discussion") renderDiscussion(); else if(state.phase==="actions") renderActions(); else renderResults(); renderDiceStage(); }
function renderPlayers() { return `<div class="player-strip">${state.players.map(player=>`<div class="player-chip ${player.index===state.turn?"turn":""} ${player.index===state.you.index?"me":""}"><strong>${player.name}</strong><span>${player.handCount}장 ${player.role?`· ${typeName(player.role)}`:""}</span></div>`).join("")}</div>`; }
function renderLobby() { const selected=state.host?localSelected:state.selected, total=selectionScore(selected), validation=validSet(selected,state.playerTarget); $("#game").innerHTML=`<section class="panel"><p class="eyebrow">대기실 · 방 코드 ${state.roomCode}</p><h2>${state.playerTarget}인 게임 — ${state.players.length}/${state.playerTarget}명 입장</h2>${renderPlayers()}${state.host?`<p class="notice">방장만 직업을 고를 수 있습니다. 인원수만큼 직업을 고른 뒤, 모두 입장하면 게임을 시작하세요.</p><div class="role-grid">${roleOptions(selected)}</div><p id="setup-notice" class="notice ${validation?"error":""}">카드 자체 점수 합: <strong>${signed(total)}점</strong> · 목표: 0 또는 ±1점<br>${validation||"선택 가능"}</p><button id="random-set" class="quiet">균형 조합</button> <button id="save-set">직업 조합 저장</button> <button id="add-bot" class="quiet" ${state.players.length>=state.playerTarget?"disabled":""}>봇 추가</button> <button id="start-game" ${state.players.length===state.playerTarget&&validation===""?"":"disabled"}>게임 시작</button>`:`<p class="notice">방장이 직업 조합을 정하는 중입니다. ${state.selected.length?`현재 ${state.selected.map(typeName).join(" · ")} · ${signed(selectionScore(state.selected))}점`:""}</p>`}</section>`;
  if(state.host) { document.querySelectorAll("[data-role]").forEach(button=>button.onclick=()=>{const key=button.dataset.role; localSelected=localSelected.includes(key)?localSelected.filter(x=>x!==key):[...localSelected,key]; renderLobby();}); $("#random-set").onclick=()=>{localSelected=randomSet(state.playerTarget);renderLobby();}; $("#save-set").onclick=()=>send("SETUP",{selected:localSelected}); $("#add-bot").onclick=()=>send("ADD_BOT"); $("#start-game").onclick=()=>send("START"); }
}
function renderRound() {
  const current = state.players[state.turn]; const final = state.round === 4; const canDiscard = state.you.canDiscard;
  const rolling = diceRoll?.rolling && diceRoll.id === state.lastRoll?.id;
  const prompt = state.required === null ? `0~${Math.min(3, state.you.hand.length)}장을 ${final ? "비공개로" : "공개로"} 버리세요.` : `주사위 결과 ${state.required}: 정확히 ${state.required}장을 반드시 버리세요.`;
  const selected = chosenCards.filter(id => state.you.hand.some(card => card.id === id)); chosenCards = selected;
  const heading = canDiscard ? "당신의 차례입니다." : `${current.name} 플레이어의 차례입니다.`;
  const secretNotice = canDiscard ? `당신의 비밀 직업: <strong>${typeName(state.you.role)}</strong> — ${ruleText(state.you.role)}` : "서버는 현재 플레이어의 손패와 비밀 직업만 전송합니다.";
  
  const opponentCards = Array.from({length: current.handCount}).map((_, i) => `<div class="card secret ${i < (current.selectedCount || 0) ? "selected selectable" : ""}" style="transform: ${i < (current.selectedCount || 0) ? 'translateY(-9px)' : 'none'}"><div class="card-name">?</div></div>`).join("");
  const controls = canDiscard ? `<div id="hand" class="card-row">${cardsHtml(state.you.hand, { selectable: true, selected })}</div><div class="status-line"><span id="selection-count" class="big">${selected.length}장 선택</span><button id="discard-confirm" ${rolling?"disabled":""}>${rolling?"주사위 확인 중…":"선택한 카드 버리기"}</button></div>` : `<div class="card-row opponent-hand">${opponentCards}</div><p class="waiting">상대가 카드를 고르는 동안 기다리세요.</p>`;
  const latestDie = state.lastRoll?.round===state.round ? `<p class="notice dice-summary">최근 D6 · <strong>${escapeHtml(state.players.find(player=>player.index===state.lastRoll.playerIndex)?.name||"플레이어")}</strong> → <strong>${state.lastRoll.value}</strong></p>` : "";
  $("#game").innerHTML = `<section class="panel"><p class="eyebrow">${state.round}라운드 ${final ? "· 마지막 비공개 버리기" : "· 공개 버리기"}</p><h2>${heading}</h2>${renderPlayers()}<p class="notice">${secretNotice}</p>${latestDie}<p class="notice">${prompt}</p>${controls}</section><section class="panel"><h3>공개 무덤</h3>${publicGrave()}</section>`;
  if (canDiscard) { document.querySelectorAll("[data-card]").forEach(button => button.onclick = () => toggleCard(button.dataset.card)); $("#discard-confirm").onclick = confirmDiscard; updateDiscardButton(); }
}
function toggleCard(id) { const max=state.required===null?Math.min(3,state.you.hand.length):state.required; chosenCards=chosenCards.includes(id)?chosenCards.filter(x=>x!==id):(chosenCards.length<max?[...chosenCards,id]:chosenCards); send("SELECT_CARD", chosenCards.length); renderRound(); }
function updateDiscardButton() { const valid=state.required===null?chosenCards.length<=3:chosenCards.length===state.required; $("#selection-count").textContent=`${chosenCards.length}장 선택${state.required===null?"":` / ${state.required}장 필수`}`; $("#discard-confirm").disabled=!valid; }
function confirmDiscard() { send("DISCARD",{cardIds:chosenCards}); chosenCards=[]; }
function publicGrave() {
  return `<div class="log">${state.players.map(player => {
    const rounds = [1, 2, 3];
    const pilesHtml = rounds.map(r => {
      const pile = player.publicDiscard.filter(c => (c.round || 1) === r);
      if(!pile.length) return '';
      return `<div class="discard-pile" data-round="${r}R">
        ${pile.map((card, idx) => `<div class="stacked-card">${cardHtml(card, {selectable: false})}</div>`).join("")}
      </div>`;
    }).join("");
    let finalPileHtml = '';
    if(player.finalDiscardCount && player.finalDiscardCount > 0) {
      finalPileHtml = `<div class="discard-pile" data-round="4R">
        ${Array.from({length: player.finalDiscardCount}).map((_, idx) => `<div class="stacked-card"><div class="card secret"></div></div>`).join("")}
      </div>`;
    }
    return `<div><strong>${escapeHtml(player.name)}</strong> 
      <div class="discard-piles-container">
        ${pilesHtml || finalPileHtml ? pilesHtml + finalPileHtml : '<span class="muted">없음</span>'}
      </div>
    </div>`;
  }).join("")}</div>`;
}
function remainingDiscussionSeconds() { return Math.max(0,Math.ceil(((state?.discussionEndsAt||Date.now())-Date.now())/1000)); }
function chatHtml() { const messages=(state.chat||[]).map(message=>`<article class="chat-message ${message.playerIndex===state.you.index?"mine":""}"><header><strong>${escapeHtml(message.name)}</strong><time>${new Date(message.sentAt).toLocaleTimeString("ko-KR",{hour:"2-digit",minute:"2-digit"})}</time><span>${message.round}라운드</span></header><p>${escapeHtml(message.text)}</p></article>`).join(""); return `<section class="chat-panel"><div class="status-line"><div><p class="eyebrow">ROUND CHAT</p><h3>토론 채팅</h3></div><span class="pill">토론 중에만 전송</span></div><div id="chat-log" class="chat-log">${messages||'<p class="muted">첫 번째 의견을 남겨 보세요.</p>'}</div><form id="chat-form" class="chat-form"><input id="chat-input" maxlength="240" autocomplete="off" placeholder="공개 카드와 추리를 이야기하세요…" required /><button>보내기</button></form></section>`; }
function refreshDiscussionClock() { const clock=$("#discussion-clock"); if(!clock||state?.phase!=="discussion") return; const seconds=remainingDiscussionSeconds(); clock.textContent=seconds>0?`${seconds}초 남음`:"다음 라운드 준비 중…"; }
function renderDiscussion() { const seconds=remainingDiscussionSeconds(); const votes=state.discussionVotes||[]; const agreed=votes.includes(state.you.index); const voters=state.players.filter(player=>votes.includes(player.index)).map(player=>escapeHtml(player.name)).join(" · ")||"아직 없음"; $("#game").innerHTML=`<section class="panel discussion-hero"><p class="eyebrow">${state.round}라운드 종료 · TABLE TALK</p><h2>토론 시간입니다</h2>${renderPlayers()}<div class="discussion-clock"><span>남은 시간</span><strong id="discussion-clock">${seconds}초 남음</strong></div><div class="skip-discussion"><div><strong>토론 종료 동의 ${votes.length}/${state.playerTarget}</strong><span>${voters}</span></div><button id="discussion-skip" class="${agreed?"quiet":""}">${agreed?"동의 취소":"토론 종료 동의"}</button></div><p class="notice">전원이 동의하면 남은 시간과 관계없이 ${state.round<4?`${state.round+1}라운드`:"직업 공개"}가 즉시 시작됩니다.</p></section><div class="discussion-grid">${chatHtml()}<section class="panel"><h3>공개 무덤</h3>${publicGrave()}</section></div>`; clearInterval(discussionTimer); discussionTimer=setInterval(refreshDiscussionClock,250); $("#discussion-skip").onclick=()=>send("SKIP_DISCUSSION"); const form=$("#chat-form"); form.onsubmit=(event)=>{event.preventDefault(); const input=$("#chat-input"); const text=input.value.trim(); if(!text) return; send("CHAT",{text}); input.value="";}; const log=$("#chat-log"); log.scrollTop=log.scrollHeight; }
function renderActions() {
  const action = state.action; 
  const isActor = state.you.canAct;
  const actor = action && action.playerIndex !== -1 ? state.players[action.playerIndex] : null;
  const formHtml = action ? actionForm(action.role, actor) : "";
  const headerText = isActor ? `내 차례입니다! ${typeName(action.role)} 능력을 사용하세요.` : `누군가 ${typeName(action.role)} 능력을 사용 중입니다...`;
  
  $("#game").innerHTML = `<section class="panel"><p class="eyebrow">직업 능력 발동 단계</p><h2>${action ? headerText : "잠시만 기다려주세요..."}</h2>${renderPlayers()}${formHtml || `<p class="notice">현재 발동 중인 능력이 없습니다.</p>`}</section><section class="panel"><h3>공개 무덤</h3>${publicGrave()}</section>`;
  
  if (isActor) bindAction(action.role);
  else if (action) {
    document.querySelectorAll(".action-grid select, .action-grid input, button#action-confirm, select#role, select#target").forEach(el => {
      if (el.tagName === "BUTTON") { el.textContent = "다른 플레이어가 고민 중입니다..."; el.disabled = true; }
      else {
        el.disabled = true;
        if (state.actionPreview && state.actionPreview[el.id]) {
          if (el.multiple) { const vals = state.actionPreview[el.id]; Array.from(el.options).forEach(opt => opt.selected = vals.includes(opt.value)); }
          else { el.value = state.actionPreview[el.id]; }
        }
      }
    });
  }
}
function actionForm(role, actor) {
  actor = actor || state.you;
  const targets = playerOptions(), hiddenTargets = playerOptions({unrevealed:true}), roles = roleSelect();
  const title = `발동 중인 능력: <strong>${typeName(role)}</strong> - ${ruleText(role)}`;
  if (role === "assassin") return `<p class="notice">${title}</p><div class="action-grid"><label>대상 1<select id="p1">${targets}</select></label><label>직업 1<select id="r1">${roles}</select></label><label>대상 2<select id="p2">${targets}</select></label><label>직업 2<select id="r2">${roles}</select></label></div><button id="action-confirm">암살하기</button>`;
  if (["slave", "hunter"].includes(role)) return `<p class="notice">${title}</p><div class="action-grid"><label>대상<select id="target">${role === "slave" ? hiddenTargets : targets}</select></label><label>직업<select id="role">${roles}</select></label></div><button id="action-confirm">지목하기</button>`;
  if (role === "chancellor") return `<p class="notice">${title}</p><label>바꿀 미공개 직업<select id="role">${roles}</select></label><button id="action-confirm">직업 변경</button>`;
  if (role === "courtesan") return `<p class="notice">${title}</p><label>유혹할 대상<select id="target">${hiddenTargets}</select></label><button id="action-confirm">유혹하기</button>`;
  if (role === "thief") return `<p class="notice">${title}</p><div class="action-grid"><label>첫번째 대상<select id="p1">${targets}</select></label><label>두번째 대상<select id="p2">${targets}</select></label></div><p class="help">한 명에게서 2장을 뺏으려면 두 칸 모두 같은 사람을 고르세요.</p><button id="action-confirm">훔치기</button>`;
  if (role === "king") {
    const indices = Array.from({length: 10}).map((_, i) => `<option value="${i}">${i+1}번째 카드</option>`).join("");
    return `<p class="notice">${title}</p><div class="action-grid"><label>지목할 상대<select id="target">${targets}</select></label><div></div><label>버리게 할 카드 1<select id="k-card1"><option value="">선택</option>${indices}</select></label><label>버리게 할 카드 2<select id="k-card2"><option value="">선택</option>${indices}</select></label></div><p class="help">상대의 실제 손패 수보다 큰 번호를 선택하면 무효 처리됩니다.</p><button id="action-confirm">버리기 확정</button>`;
  }
  if (role === "queen") {
    const handCards = actor.id === state.you.id ? state.you.hand : [];
    const handOptions = handCards.map(card => `<option value="${card.id}">${typeName(card.type)} (${signed(ROLE_DATA[card.type].score)})</option>`).join("");
    const pubOptions = state.players.flatMap(player => player.publicDiscard.map(card => `<option value="${card.id}">${player.name}의 ${typeName(card.type)} (${signed(ROLE_DATA[card.type].score)})</option>`)).join("");
    const secOptions = state.players.flatMap(player => Array.from({length: player.finalDiscardCount || 0}).map((_, i) => `<option value="secret-${player.id}-${i}">${player.name}의 비공개 무덤 ${i+1}</option>`)).join("");
    return `<p class="notice">${title}</p><div class="action-grid"><label class="wide">교환 방식<select id="q-type"><option value="">선택 안함</option><option value="public">공개 무덤 1장 교환</option><option value="secret">비공개 무덤 2장 교환 (무작위)</option></select></label><label>버릴 손패 1<select id="q-own1"><option value="">선택 안함</option>${handOptions}</select></label><label>가져올 무덤 1<select id="q-grave1"><option value="">선택 안함</option><optgroup label="공개 무덤">${pubOptions}</optgroup><optgroup label="비공개 무덤">${secOptions}</optgroup></select></label><label>버릴 손패 2<select id="q-own2"><option value="">선택 안함</option>${handOptions}</select></label><label>가져올 무덤 2<select id="q-grave2"><option value="">선택 안함</option><optgroup label="비공개 무덤">${secOptions}</optgroup></select></label></div><button id="action-confirm">교환하기</button>`;
  }
  if (role === "mercenary") {
    const handCards = actor.id === state.you.id ? state.you.hand : [];
    const handOptions = handCards.map(card => `<option value="${card.id}">${typeName(card.type)} (${signed(ROLE_DATA[card.type].score)})</option>`).join("");
    const graveOptions = state.players.flatMap(player => player.publicDiscard.map(card => `<option value="${card.id}">${player.name}의 ${typeName(card.type)} (${signed(ROLE_DATA[card.type].score)})</option>`)).join("");
    return `<p class="notice">${title}</p><p class="help">멀티셀렉트(다중선택)로 4장을 고르세요. (모바일은 여러 항목 터치, PC는 Ctrl+클릭)</p><div class="action-grid mercenary-form"><label class="wide">목표 손패 4장<select id="m-group" multiple size="6">${handOptions || '<option value="" disabled>본인만 보입니다</option>'}</select></label><label>버릴 1번째패<select id="m-own1"><option value="">선택 안함</option>${handOptions}</select></label><label>가져올 1번째패<select id="m-grave1"><option value="">선택 안함</option>${graveOptions}</select></label><label>버릴 2번째패<select id="m-own2"><option value="">선택 안함</option>${handOptions}</select></label><label>가져올 2번째패<select id="m-grave2"><option value="">선택 안함</option>${graveOptions}</select></label></div><button id="action-confirm">목표 달성 시도하기</button>`;
  }
  return `<p class="notice">${title}</p><button id="action-confirm">능력 확인</button>`;
}
function bindAction(role) {
  const inputs = document.querySelectorAll(".action-grid select, .action-grid input, select#role, select#target");
  inputs.forEach(el => {
    el.addEventListener("change", () => {
      const preview = {};
      inputs.forEach(input => { if (input.multiple) { preview[input.id] = Array.from(input.selectedOptions).map(opt => opt.value); } else { preview[input.id] = input.value; } });
      send("ACTION_PREVIEW", preview);
    });
  });
  if (role === "queen") {
    $("#action-confirm").onclick = () => {
      const type = $("#q-type").value;
      if (!type) return toast("교환 방식을 먼저 선택하세요.");
      const swaps = [];
      const own1 = $("#q-own1").value, grave1 = $("#q-grave1").value;
      const own2 = $("#q-own2").value, grave2 = $("#q-grave2").value;
      if (type === "public") {
        if (!own1 || !own2 || !grave1) return toast("버릴 손패 2장과 공개 무덤 카드 1장을 선택하세요.");
        if (own1 === own2) return toast("서로 다른 손패 카드를 버려야 합니다.");
        if (grave2) return toast("공개 무덤은 1장만 교환 가능합니다.");
        swaps.push({ ownId: own1, graveId: grave1 }, { ownId: own2, graveId: null });
      } else {
        if (!own1 || !grave1 || !own2 || !grave2) return toast("비공개 무덤 2장 교환 시, 버릴 카드 2장과 가져올 2장을 모두 선택하세요.");
        if (own1 === own2) return toast("서로 다른 손패 카드를 버려야 합니다.");
        if (grave1 === grave2) return toast("비공개 무덤에서 서로 다른 카드를 지정해야 합니다.");
        swaps.push({ ownId: own1, graveId: grave1 }, { ownId: own2, graveId: grave2 });
      }
      send("ACTION", { type, swaps });
    };
    return;
  }
  if (role === "mercenary") { $("#action-confirm").onclick = () => { const group = [...$("#m-group").selectedOptions].map(option => option.value); if (group.length !== 4) return toast("남길 패를 4장 선택하세요."); const swaps = []; for (const index of [1, 2]) { const ownId = $("#m-own" + index).value, graveId = $("#m-grave" + index).value; if (!ownId && !graveId) continue; if (!ownId || !graveId) return toast("교환할 카드와 무덤 카드를 모두 선택하거나 모두 비워야 합니다."); swaps.push({ ownId, graveId }); } send("ACTION", { group, swaps }); }; return; }
  $("#action-confirm").onclick = () => { 
    if (role === "assassin") return send("ACTION", { guesses: [{ player: +$("#p1").value, role: $("#r1").value }, { player: +$("#p2").value, role: $("#r2").value }] }); 
    if (["slave", "hunter"].includes(role)) return send("ACTION", { player: +$("#target").value, role: $("#role").value }); 
    if (role === "chancellor") return send("ACTION", { role: $("#role").value }); 
    if (role === "courtesan") return send("ACTION", { player: +$("#target").value }); 
    if (role === "thief") return send("ACTION", { players: [+($("#p1").value), +($("#p2").value)] }); 
    if (role === "king") {
      const idx1 = $("#k-card1").value, idx2 = $("#k-card2").value;
      if (idx1 === "" || idx2 === "") return toast("버리게 할 2장을 선택하세요.");
      if (idx1 === idx2) return toast("서로 다른 카드를 선택하세요.");
      return send("ACTION", { player: +$("#target").value, indices: [+idx1, +idx2] });
    }
  };
}
function renderResults() { const rows=state.results.map((result,index)=>`<tr><td class="rank">${index+1}</td><td>${result.name}</td><td><strong>${typeName(result.role)}</strong></td><td>${result.hand.map(card=>typeName(card.type)).join(", ")||"없음"}</td><td>${signed(result.base)} ${result.detail?`<small class="muted">${result.detail}</small>`:""}</td><td>${result.success?`+${result.bonus} 성공`:"0 실패"}</td><td class="rank">${result.total}</td></tr>`).join(""); $("#game").innerHTML=`<section class="panel"><p class="eyebrow">결산</p><h2>모든 손패와 직업이 공개되었습니다.</h2><p class="notice">4라운드 비공개 버림 카드의 종류는 공개하지 않았습니다. 서버가 조건 판정에 필요한 카드만 확인했습니다.</p><table class="summary-table"><thead><tr><th>순위</th><th>플레이어</th><th>직업</th><th>최종 손패</th><th>카드 점수</th><th>직업 조건</th><th>총점</th></tr></thead><tbody>${rows}</tbody></table></section>`; }
function refreshGraveDialog() { const modal=$(".grave-modal"); if(!modal)return; if(!state||state.phase==="lobby")return modal.remove(); modal.querySelector(".grave-live-list").innerHTML=publicGrave(); }
function openGrave() { if(!state||state.phase==="lobby"||$(".grave-modal"))return; const modal=document.createElement("section");modal.className="grave-modal";modal.setAttribute("role","dialog");modal.setAttribute("aria-label","공개 무덤");modal.innerHTML=`<div class="grave-dialog panel"><div class="status-line"><div><p class="eyebrow">언제든 확인 가능</p><h2>공개 무덤</h2></div><button id="close-grave" class="quiet">닫기</button></div><p class="help">1~3라운드 공개 카드입니다. 4라운드 비공개 더미는 표시하지 않습니다.</p><div class="grave-live-list">${publicGrave()}</div></div>`;document.body.append(modal);$("#close-grave").onclick=()=>modal.remove();}
$("#grave-button").onclick=openGrave; $("#leave-button").onclick=()=>{if(socket)socket.close();socket=null;state=null;chosenCards=[];localSelected=[];diceRoll=null;clearTimeout(diceTimer);clearInterval(discussionTimer);render();}; renderLanding();
