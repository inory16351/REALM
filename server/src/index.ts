import { DurableObject } from "cloudflare:workers";

type RoleKey = keyof typeof ROLE_DATA;
type Card = { id: string; type: RoleKey };
type Phase = "lobby" | "round" | "discussion" | "actions" | "accusation" | "results";
type Accusation = { player: number; role: RoleKey };
type Player = { id: string; name: string; index: number; role: RoleKey | null; hand: Card[]; publicDiscard: Card[]; finalDiscard: Card[]; forged: Card[]; action: Record<string, unknown>; revealed: boolean; isBot?: boolean; acting?: boolean; selectedCount?: number; accusations?: Accusation[]; accused?: boolean; };
type Baseline = { hands: Card[][]; discardCounts: Record<string, number>; handCounts: Record<string, number>; finalByPlayer: Card[][]; publicByPlayer: Card[][] };
type ChatMessage = { id: string; playerIndex: number; name: string; text: string; round: number; sentAt: number };
type DieRoll = { id: string; playerIndex: number; value: number; faceIndex: number; round: number; rolledAt: number };
type Room = { code: string; hostId: string; playerTarget: number; discussionSeconds: number; discussionEndsAt: number | null; discussionVotes: string[]; chat: ChatMessage[]; lastRoll: DieRoll | null; players: Player[]; selected: RoleKey[]; removed?: Card[]; phase: Phase; round: number; turn: number; turnOrder: number[]; turnCursor: number; required: number | null; actionCursor: number; baseline: Baseline | null; log: string[] };
type Evaluation = { success: boolean; base: number; bonus: number; accusePoints: number; accuseCorrect: number; exposedBy: number; total: number; detail: string };

const ROLE_DATA: Record<RoleKey, { name: string; score: number; bonus: number; rule: string }> = {
	king: { name: "국왕", score: -1, bonus: 9, rule: "종료 직전 다른 유저를 지목해, 그 사람의 손패 위치 2곳을 지정해 버리게 합니다. (손패는 비공개라 내용을 보지 못합니다.) 전체 무덤에 왕이 9장 이상이거나(투입량 적을 시) 전원의 손패에 왕이 없으면 성공." },
	noble: { name: "귀족", score: 1, bonus: 7, rule: "모든 플레이어가 4라운드에 비공개로 버린 카드 중 귀족이 정확히 3장이면 성공합니다." },
	assassin: { name: "암살자", score: 0, bonus: 10, rule: "최종 지목에서 서로 다른 상대 두 명을 지목합니다. 두 명의 직업을 모두 맞히면 성공합니다." },
	beggar: { name: "거지", score: -1, bonus: 10, rule: "4라운드 직후 전원의 남은 손패 합계가 인원수×2장보다 적으면 성공합니다." },
	slave: { name: "노예", score: 0, bonus: 7, rule: "최종 지목에서 직업을 맞히고, 그 직업이 실제로 성공까지 해야 성공합니다." },
	jester: { name: "광대", score: -1, bonus: 8, rule: "최종 지목에서 암살자가 나를 지목하거나, 나를 지목한 사람이 3명(5인전은 2명) 이상이면 성공합니다. 지목된 직업이 맞는지는 상관없습니다. 2·3라운드의 의심은 포함되지 않습니다." },
	priest: { name: "사제", score: 1, bonus: 8, rule: "모든 플레이어가(본인 포함) 손패에 사제를 최소 1장 이상 가지고 있으면 성공합니다." },
	knight: { name: "기사", score: 1, bonus: 7, rule: "전체 무덤(공개+비공개)에 버려진 기사 수가, 버려진 다른 어떤 카드 종류의 수보다 적거나 같으면 성공합니다." },
	bard: { name: "음유시인", score: 0, bonus: 9, rule: "마지막 손패가 모두 음유시인이고(최소 1장 이상), 자신이 4라운드에 비공개로 버린 카드 중 음유시인이 최소 1장 있으면 성공합니다." },
	hunter: { name: "사냥꾼", score: 0, bonus: 8, rule: "상대 한 명과 특정 카드를 지목합니다. 그 상대의 손패에 지목한 카드가 최소 1장 있으면 성공합니다." },
	commoner: { name: "평민", score: 1, bonus: 7, rule: "전원의 남은 손패에 있는 평민 카드의 수가, 다른 어떤 카드의 수보다 많거나 같으면 성공합니다." },
	merchant: { name: "상인", score: 1, bonus: 6, rule: "4라운드 직후 전원의 남은 손패 합계가 인원수×2장보다 많으면 성공합니다." },
	blacksmith: { name: "대장장이", score: 0, bonus: 0, rule: "선택할 것이 없습니다. 전원의 비공개 무덤에서 무작위 3장을 단조하며, 단조한 카드의 점수 총합과 손패 점수 총합을 더한 절댓값의 2배가 내 점수가 됩니다 (최대 12점)." },
	thief: { name: "도적", score: 0, bonus: 9, rule: "대상과 그 사람의 손패 위치를 지정해 2장을 훔쳐 내 손패에 넣습니다. 한 명을 두 번 지정해도 됩니다. 훔친 2장의 종류가 같으면 성공합니다. (손패는 비공개라 내용을 보지 못합니다.)" },
	mercenary: { name: "용병", score: 0, bonus: 9, rule: "손패에서 목표 카드 4장을 정하고, 공개 무덤과 최대 2장까지 교환합니다. 교환 후 목표 4장이 모두 같은 종류면 성공합니다." },
	seer: { name: "예언자", score: 0, bonus: 7, rule: "최종 손패가 2장 이상이고, 모두 같은 종류의 카드면 성공합니다." },
	alchemist: { name: "연금술사", score: -1, bonus: 8, rule: "마지막 손패에 원래 점수가 -1점, 0점, +1점인 카드가 각각 최소 1장씩 모두 존재하면 성공합니다." },
	librarian: { name: "사서", score: 1, bonus: 10, rule: "마지막 손패에 서로 다른 종류의 카드가 4종 이상 있으면 성공합니다." },
	mage: { name: "마법사", score: 0, bonus: 8, rule: "마지막 손패가 3장 이상이고, 손패의 원래 점수 합이 정확히 0점이면 성공합니다." },
	farmer: { name: "농부", score: 1, bonus: 7, rule: "마지막 손패가 5장 이상이고, 손패에 농부 카드가 3장 이상 있으면 성공합니다." },
	courtesan: { name: "매춘부", score: 0, bonus: 8, rule: "지목한 플레이어의 손패 중 첫 번째 카드가 전체 게임에 남은 동일한 종류의 카드 중 가장 많은 카드 종류라면 성공합니다." },
	pope: { name: "교황", score: 1, bonus: 8, rule: "전체 무덤에 교황이 6장 이상 버려져 있고, 내 마지막 손패에 교황이 최소 1장 있으면 성공합니다." },
	barbarian: { name: "야만인", score: -1, bonus: 8, rule: "1~3라운드에 나를 포함해 누구보다 많은 카드를 버렸다면(최소 5장) 성공합니다. (공동 1위도 인정)" },
	chancellor: { name: "재상", score: 1, bonus: 7, rule: "지목한 카드 종류가 전원의 손패 중 가장 많다면 성공합니다. (공동 1위도 인정)" },
	queen: { name: "왕비", score: 1, bonus: 7, rule: "손패에서 2장을 버리고 공개 무덤 1장 혹은 비공개 무덤 2장(무작위)을 가져옵니다. 마지막 손패의 왕비가 2장 이상이고, 다른 모든 플레이어보다 많아야 성공합니다. (동점은 실패)" },
} as const;
const ROLE_KEYS = Object.keys(ROLE_DATA) as RoleKey[];
// assassin and slave moved to the accusation phase: both are pure identity
// guesses, which is exactly what every player now does at the end.
const ACTION_ORDER: RoleKey[] = ["hunter", "chancellor", "courtesan", "thief", "king", "queen", "mercenary", "blacksmith"];
// A correct final accusation pays a flat 3 no matter how many of your guesses
// landed, so the assassin's two picks are rewarded through their role bonus
// rather than by doubling this. Being correctly identified costs 1 per person.
const ACCUSE_CORRECT = 3;
const ACCUSE_EXPOSED_EACH = -1;
// How many accusers the jester needs when the assassin does not name them.
// Flat 3 everywhere except a 5-player table, where 2 keeps it reachable.
// The expected number of accusers on any one player is ~1.1 regardless of
// table size (more accusers, but more targets too), so this is a real bar
// rather than something that drifts with player count.
const JESTER_THRESHOLD: Record<number, number> = { 5: 2, 6: 3, 7: 3, 8: 3, 9: 3, 10: 3 };
const ACCUSE_EXPOSED = -2;
const D6_DISCARD_FACES = [0, 1, 1, 2, 2, 3] as const;
const DISCUSSION_SECONDS = new Set([15, 30, 45, 60, 90, 120, 180]);

const json = (value: unknown, init: ResponseInit = {}) => new Response(JSON.stringify(value), { ...init, headers: { "content-type": "application/json; charset=utf-8", ...(init.headers || {}) } });
const bad = (message: string, status = 400) => json({ error: message }, { status });
const parseCookie = (request: Request, name: string) => request.headers.get("Cookie")?.split(";").map((part) => part.trim()).find((part) => part.startsWith(`${name}=`))?.slice(name.length + 1);
const randomId = (length = 24) => Array.from(crypto.getRandomValues(new Uint8Array(length)), (byte) => byte.toString(16).padStart(2, "0")).join("");
const randomInt = (max: number) => { const ceiling = Math.floor(0x1_0000_0000 / max) * max; const bytes = new Uint32Array(1); do crypto.getRandomValues(bytes); while (bytes[0] >= ceiling); return bytes[0] % max; };
const shuffle = <T>(items: T[]) => { const output = [...items]; for (let i = output.length - 1; i > 0; i--) { const j = randomInt(i + 1); [output[i], output[j]] = [output[j], output[i]]; } return output; };
const countTypes = (cards: Card[], roles: RoleKey[]) => { const counts: Record<string, number> = Object.fromEntries(roles.map((role) => [role, 0])); for (const card of cards) counts[card.type] = (counts[card.type] || 0) + 1; return counts; };
const cardCount = (cards: Card[], role: RoleKey) => cards.filter((card) => card.type === role).length;

export class GameRoom extends DurableObject<Env> {
	private room: Room | null = null;
	constructor(ctx: DurableObjectState, env: Env) { super(ctx, env); ctx.blockConcurrencyWhile(async () => { this.room = (await ctx.storage.get<Room>("room")) ?? null; if (this.room) { this.room.discussionSeconds ??= 60; this.room.discussionEndsAt ??= null; this.room.discussionVotes ??= []; this.room.chat ??= []; this.room.lastRoll ??= null; this.room.turnOrder ??= []; this.room.turnCursor ??= 0; } }); }

	async fetch(request: Request): Promise<Response> {
		const url = new URL(request.url); const sessionId = request.headers.get("X-Player-Session") || parseCookie(request, "crown_session");
		if (url.pathname === "/join" && request.method === "POST") return this.join(request, sessionId);
		if (url.pathname === "/state" && request.method === "GET") return this.stateFor(sessionId);
		if (url.pathname === "/ws") return this.openSocket(request, sessionId);
		return bad("없는 방 요청입니다.", 404);
	}

	private async join(request: Request, sessionId?: string): Promise<Response> {
		if (!sessionId) return bad("세션이 없습니다.", 401);
		const body = await request.json<{ name?: string; playerTarget?: number; discussionSeconds?: number; code?: string }>(); const name = (body.name || "").trim().slice(0, 16);
		if (!name) return bad("플레이어 이름을 입력하세요.");
		if (!this.room) { const target = Number(body.playerTarget); const discussionSeconds = Number(body.discussionSeconds ?? 60); if (!Number.isInteger(target) || target < 5 || target > 10 || !body.code) return bad("시작 인원은 5~10명이어야 합니다."); if (!DISCUSSION_SECONDS.has(discussionSeconds)) return bad("토론 시간은 15·30·45·60·90·120·180초 중 하나여야 합니다."); this.room = { code: body.code, hostId: sessionId, playerTarget: target, discussionSeconds, discussionEndsAt: null, discussionVotes: [], chat: [], lastRoll: null, players: [], selected: [], removed: [], phase: "lobby", round: 0, turn: 0, turnOrder: [], turnCursor: 0, required: null, actionCursor: 0, baseline: null, log: [`방이 열렸습니다. 라운드 토론 시간: ${discussionSeconds}초.`] }; }
		const room = this.room; const existing = room.players.find((player) => player.id === sessionId);
		if (!existing) { if (room.phase !== "lobby") return bad("이미 시작한 게임에는 참가할 수 없습니다.", 409); if (room.players.length >= room.playerTarget) return bad("방이 가득 찼습니다.", 409); room.players.push({ id: sessionId, name, index: room.players.length, role: null, hand: [], publicDiscard: [], finalDiscard: [], forged: [], action: {}, revealed: false }); room.log.push(`${name} 플레이어가 입장했습니다.`); }
		await this.saveAndBroadcast(); return this.stateFor(sessionId);
	}

	private async openSocket(request: Request, sessionId?: string): Promise<Response> {
		if (!sessionId || !this.room?.players.some((player) => player.id === sessionId)) return bad("먼저 방에 참가하세요.", 401);
		if (request.headers.get("Upgrade") !== "websocket") return bad("WebSocket 연결이 필요합니다.", 426);
		const pair = new WebSocketPair(); const [client, server] = Object.values(pair); server.serializeAttachment(sessionId); this.ctx.acceptWebSocket(server, [sessionId]); server.send(JSON.stringify({ type: "STATE", state: this.viewFor(sessionId) })); return new Response(null, { status: 101, webSocket: client });
	}

	async webSocketMessage(socket: WebSocket, message: string | ArrayBuffer): Promise<void> {
		try { const sessionId = socket.deserializeAttachment() as string | null; const event = JSON.parse(typeof message === "string" ? message : new TextDecoder().decode(message)) as { type?: string; payload?: unknown }; if (!sessionId || !event.type) throw new Error("잘못된 요청입니다."); await this.handleCommand(sessionId, event.type, event.payload); }
		catch (error) { socket.send(JSON.stringify({ type: "ERROR", message: error instanceof Error ? error.message : "요청을 처리할 수 없습니다." })); }
	}
	async webSocketClose(socket: WebSocket): Promise<void> { socket.close(1000, "closed"); }

	private async handleCommand(sessionId: string, type: string, payload: unknown) {
		const room = this.requireRoom(); const player = this.requirePlayer(sessionId);
		if (type === "SETUP") { if (room.hostId !== sessionId || room.phase !== "lobby") throw new Error("방장만 대기실 설정을 바꿀 수 있습니다."); const selected = [...new Set((payload as { selected?: RoleKey[] }).selected || [])]; const problem = this.validateSelection(selected); if (problem) throw new Error(problem); room.selected = selected; room.log.push("방장이 이번 판의 직업을 정했습니다."); }
		else if (type === "ADD_BOT") { if (room.hostId !== sessionId || room.phase !== "lobby") throw new Error("방장만 봇을 추가할 수 있습니다."); if (room.players.length >= room.playerTarget) throw new Error("방이 가득 찼습니다."); const botId = `bot-${randomId(6)}`; room.players.push({ id: botId, name: `봇 ${room.players.length + 1}`, index: room.players.length, role: null, hand: [], publicDiscard: [], finalDiscard: [], forged: [], action: {}, revealed: false, isBot: true }); room.log.push(`봇이 입장했습니다.`); }
		else if (type === "START") { if (room.hostId !== sessionId || room.phase !== "lobby") throw new Error("방장만 게임을 시작할 수 있습니다."); if (room.players.length !== room.playerTarget) throw new Error(`플레이어 ${room.playerTarget}명이 모두 입장해야 합니다.`); const problem = this.validateSelection(room.selected); if (problem) throw new Error(problem); this.startGame(room); }
		else if (type === "ACCUSE") {
			if (room.phase !== "accusation") throw new Error("지금은 최종 지목 단계가 아닙니다.");
			if (player.accused) throw new Error("이미 지목을 마쳤습니다.");
			const quota = this.accusationQuota(player);
			const raw = Array.isArray((payload as { guesses?: unknown }).guesses) ? (payload as { guesses: unknown[] }).guesses : [];
			if (raw.length !== quota) throw new Error(`정확히 ${quota}명을 지목해야 합니다.`);
			const parsed = raw.map((entry) => {
				const record = entry as Record<string, unknown>;
				const victim = room.players.find((candidate) => candidate.index === Number(record.player));
				const guessed = ROLE_DATA[record.role as RoleKey] ? (record.role as RoleKey) : null;
				if (!victim || victim.index === player.index || !guessed) throw new Error("자신이 아닌 상대와 직업을 지목해야 합니다.");
				return { player: victim.index, role: guessed };
			});
			if (parsed.length === 2 && parsed[0].player === parsed[1].player) throw new Error("서로 다른 두 명을 지목해야 합니다.");
			player.accusations = parsed; player.accused = true;
			room.log.push(`${player.name} 플레이어가 지목을 마쳤습니다.`);
			this.finishAccusationIfReady(room);
		}
		else if (type === "SELECT_CARD") { if (room.phase !== "round" || room.players[room.turn].id !== player.id) return; player.selectedCount = Number(payload); }
		else if (type === "DISCARD") { if (room.phase !== "round" || room.players[room.turn].id !== player.id) throw new Error("지금은 당신의 버리기 차례가 아닙니다."); player.selectedCount = 0; await this.discard(room, player, payload as { cardIds?: string[] }); }
		else if (type === "CHAT") { if (room.phase !== "discussion") throw new Error("채팅은 라운드 토론 시간에만 보낼 수 있습니다."); this.addChat(room, player, payload); }
		else if (type === "SKIP_DISCUSSION") { if (room.phase !== "discussion") throw new Error("토론 단계에서만 종료 동의를 누를 수 있습니다."); this.voteToSkipDiscussion(room, player); }
		else if (type === "ACTION_PREVIEW") { if (room.phase !== "actions" || this.currentActionPlayer()?.id !== player.id) return; room.actionPreview = payload as Record<string, string>; }
		else if (type === "ACTION") { if (room.phase !== "actions" || this.currentActionPlayer()?.id !== player.id) throw new Error("지금은 당신의 직업 능력 차례가 아닙니다."); this.resolveAction(room, player, payload as Record<string, unknown>); room.actionPreview = undefined; }
		else throw new Error("알 수 없는 명령입니다.");
		await this.saveAndBroadcast();
		this.ctx.waitUntil(this.checkBots());
	}

	private async checkBots() {
		const room = this.room; if (!room) return;
		let acted = false;

		if (room.phase === "round") {
			const current = room.players[room.turn];
			if (current.isBot && !current.acting) {
				current.acting = true;
				await this.saveAndBroadcast();
				
				await new Promise((r) => setTimeout(r, 1200));
				let count = room.required;
				if (count === null) count = randomInt(Math.min(3, current.hand.length) + 1);
				
				const ownRoleCards = current.hand.filter((c) => c.type === current.role);
				const otherCards = current.hand.filter((c) => c.type !== current.role);
				let chosen: Card[] = [];
				if (count > 0) {
					if (otherCards.length >= count) {
						chosen = shuffle(otherCards).slice(0, count);
					} else if (ownRoleCards.length >= count && count >= 2) {
						chosen = ownRoleCards.slice(0, count);
					} else {
						chosen = current.hand.slice(0, count);
					}
				}
				
				for (let i = 1; i <= chosen.length; i++) {
					await new Promise(r => setTimeout(r, 600));
					current.selectedCount = i;
					await this.saveAndBroadcast();
				}
				await new Promise(r => setTimeout(r, 800));
				
				current.selectedCount = 0;
				current.acting = false;
				try {
					await this.discard(room, current, { cardIds: chosen.map((c) => c.id) });
				} catch (e) {
					if (room.required === null) await this.discard(room, current, { cardIds: [] });
					else {
                        try {
                            const fallback = otherCards.length >= count ? otherCards.slice(0, count) : ownRoleCards.length >= count && count >= 2 ? ownRoleCards.slice(0, count) : current.hand.slice(0, count);
                            await this.discard(room, current, { cardIds: fallback.map(c => c.id) });
                        } catch(e2) {
                            await this.advanceTurn(room);
                        }
                    }
				}
				acted = true;
			}
		} else if (room.phase === "discussion") {
			const bots = room.players.filter(p => p.isBot && !room.discussionVotes.includes(p.id));
			if (bots.length > 0) {
				if (Math.random() < 0.2) {
					await new Promise(r => setTimeout(r, 1500));
					this.voteToSkipDiscussion(room, bots[0]);
					acted = true;
				} else {
					await new Promise(r => setTimeout(r, 2000));
					this.ctx.waitUntil(this.checkBots()); // Re-evaluate voting later
				}
			}
		} else if (room.phase === "actions") {
			const actor = this.currentActionPlayer();
			if (actor && actor.isBot && !actor.acting) {
				actor.acting = true;
				await new Promise(r => setTimeout(r, 1500));
				actor.acting = false;
				this.resolveBotAction(room, actor);
				acted = true;
			}
		}

		if (acted) {
			await this.saveAndBroadcast();
			this.ctx.waitUntil(this.checkBots());
		}
	}

	private resolveBotAction(room: Room, bot: Player) {
		const role = ACTION_ORDER[room.actionCursor];
		const randomTarget = () => shuffle(room.players.filter(p => p.id !== bot.id))[0];
		const randomRole = () => shuffle(room.selected)[0];
		try {
			if (role === "assassin") {
				const targets = shuffle(room.players.filter(p => p.id !== bot.id)).slice(0, 2);
				this.resolveAction(room, bot, { guesses: [{ player: targets[0].index, role: randomRole() }, { player: targets[1].index, role: randomRole() }] });
			} else if (role === "slave" || role === "hunter") {
				const target = shuffle(room.players.filter(p => p.id !== bot.id && !p.revealed))[0] || randomTarget();
				this.resolveAction(room, bot, { player: target.index, role: randomRole() });
			} else if (role === "chancellor") {
				this.resolveAction(room, bot, { role: randomRole() });
			} else if (role === "courtesan") {
				const target = shuffle(room.players.filter(p => p.id !== bot.id && !p.revealed))[0] || randomTarget();
				this.resolveAction(room, bot, { player: target.index });
			} else if (role === "thief") {
				const targets = shuffle(room.players.filter(p => p.id !== bot.id && p.hand.length > 0));
				if (!targets.length) { room.actionCursor++; this.advanceAutomaticActions(room); return; }
				const picks = targets[1]
					? [{ player: targets[0].index, index: 0 }, { player: targets[1].index, index: 0 }]
					: [{ player: targets[0].index, index: 0 }, { player: targets[0].index, index: 1 }];
				this.resolveAction(room, bot, { picks });
			} else if (role === "king") {
				const validTargets = room.players.filter(p => p.id !== bot.id && p.hand.length >= 2);
				if (validTargets.length > 0) {
					const target = shuffle(validTargets)[0];
					this.resolveAction(room, bot, { player: target.index, indices: [0, 1] });
				} else {
					const target = shuffle(room.players.filter(p => p.id !== bot.id))[0];
					if (target && target.hand.length >= 2) this.resolveAction(room, bot, { player: target.index, indices: [0, 1] });
					else { room.actionCursor++; this.advanceAutomaticActions(room); return; }
				}
			} else if (role === "queen") {
				const validGraveCards = room.players.flatMap(p => p.publicDiscard);
				if (validGraveCards.length > 0 && bot.hand.length >= 2) {
					this.resolveAction(room, bot, { type: "public", swaps: [{ ownId: bot.hand[0].id, graveId: shuffle(validGraveCards)[0].id }, { ownId: bot.hand[1].id, graveId: null }] });
				} else {
					this.resolveAction(room, bot, { type: "secret", swaps: [] }); // dummy fallback
				}
			} else if (role === "mercenary") {
				const group = shuffle(bot.hand).slice(0, 4).map(c => c.id);
				this.resolveAction(room, bot, { group, swaps: [] });
			}
		} catch (e) {
			room.actionCursor++;
			this.advanceAutomaticActions(room);
		}
	}

	private validateSelection(selected: RoleKey[]) {
		const room = this.requireRoom();
		if (selected.length !== room.playerTarget || selected.some((role) => !ROLE_DATA[role])) return `직업을 정확히 ${room.playerTarget}개 골라야 합니다.`;
		const scores = selected.map((role) => ROLE_DATA[role].score);
		const negatives = scores.filter((score) => score < 0).length;
		const positives = scores.filter((score) => score > 0).length;
		const zeros = scores.filter((score) => score === 0).length;
		const limit = Math.ceil(room.playerTarget * 0.4);
		const total = scores.reduce<number>((sum, score) => sum + score, 0);
		if (negatives < 1 || negatives > limit || positives < 1 || positives > limit || zeros < 1) return `균형을 위해 -1점·+1점 직업은 각각 1~${limit}개, 0점 직업은 1개 이상 필요합니다.`;
		if (Math.abs(total) > 1) return `선택한 카드 자체 점수 합은 0 또는 ±1이어야 합니다. 현재 ${total > 0 ? "+" : ""}${total}점입니다.`;
		if (selected.includes("jester") && !selected.includes("assassin")) return "광대가 있으면 암살자를 함께 골라야 합니다.";
		return "";
	}
	private startGame(room: Room) {
		let deck: Card[] = room.selected.flatMap((type) => Array.from({ length: 12 }, (_, number) => ({ id: `${type}-${number}`, type })));
		deck = shuffle(deck); room.removed = deck.splice(0, room.playerTarget * 2);
		const roles = shuffle(room.selected);
		room.players.forEach((player, index) => { player.role = roles[index]; player.hand = deck.splice(0, 10); player.publicDiscard = []; player.finalDiscard = []; player.forged = []; player.action = {}; player.revealed = false; player.selectedCount = 0; player.acting = false; });
		room.round = 1; room.phase = "round"; room.chat = []; room.discussionEndsAt = null; room.discussionVotes = []; room.lastRoll = null;
		this.beginRound(room);
		room.log.push("게임 시작. 1라운드 첫 번째 주사위를 굴렸습니다.");
	}
	private rollDiscardDie(room: Room, playerIndex: number) {
		const faceIndex = randomInt(6); const value = D6_DISCARD_FACES[faceIndex];
		room.required = value;
		room.lastRoll = { id: randomId(6), playerIndex, value, faceIndex, round: room.round, rolledAt: Date.now() };
		room.log.push(`${room.round}라운드: ${room.players[playerIndex].name} 플레이어의 D6 결과는 ${value}입니다.`);
	}
	private beginPlayerTurn(room: Room, playerIndex: number) {
		room.turn = playerIndex; room.phase = "round";
		if (room.round === 1 || room.round === 3) this.rollDiscardDie(room, playerIndex);
		else room.required = null;
	}
	// Turn order is reshuffled every round, so the host no longer always leads.
	private beginRound(room: Room) {
		room.turnOrder = shuffle(room.players.map((player) => player.index));
		room.turnCursor = 0;
		room.log.push(`${room.round}라운드 순서: ${room.turnOrder.map((index) => room.players[index]?.name ?? "?").join(" → ")}`);
		this.beginPlayerTurn(room, room.turnOrder[0] ?? 0);
	}
	private async discard(room: Room, player: Player, payload: { cardIds?: string[] }) {
		const ids = Array.isArray(payload.cardIds) ? [...new Set(payload.cardIds)] : [];
		const max = Math.min(3, player.hand.length);
		if ((room.required !== null && ids.length !== room.required) || (room.required === null && ids.length > max)) throw new Error(room.required === null ? `0~${max}장만 버릴 수 있습니다.` : `주사위 결과 ${room.required}장만큼 반드시 버려야 합니다.`);
		const chosen = player.hand.filter((card) => ids.includes(card.id));
		if (chosen.length !== ids.length) throw new Error("손패에 없는 카드를 선택했습니다.");

		const hasOwnRole = chosen.some(card => card.type === player.role);
		if (hasOwnRole) {
			if (chosen.length < 2) throw new Error(`자신의 직업(${ROLE_DATA[player.role!].name}) 카드를 버리려면 2장 이상 버려야 합니다.`);
			if (!chosen.every(card => card.type === player.role)) throw new Error(`자신의 직업(${ROLE_DATA[player.role!].name}) 카드를 버릴 때는 반드시 그 카드로만 구성해서 버려야 합니다.`);
		}

		chosen.forEach((c) => { c.round = room.round; });

		player.hand = player.hand.filter((card) => !ids.includes(card.id));
		if (room.round === 4) player.finalDiscard.push(...chosen); else player.publicDiscard.push(...chosen);
		room.log.push(`${room.round}라운드: ${player.name} 플레이어가 ${chosen.length}장을 ${room.round === 4 ? "비공개로" : "공개로"} 버렸습니다.`);
		await this.advanceTurn(room);
	}
	private async beginDiscussion(room: Room) {
		room.phase = "discussion"; room.required = null;
		room.discussionVotes = [];
		room.discussionEndsAt = Date.now() + room.discussionSeconds * 1000;
		room.log.push(`${room.round}라운드가 끝났습니다. ${room.discussionSeconds}초 동안 토론합니다.`);
		await this.ctx.storage.setAlarm(room.discussionEndsAt);
	}
	private beginActions(room: Room) {
		const hands = room.players.map((player) => [...player.hand]);
		const allDiscard = room.players.flatMap((player) => [...player.publicDiscard, ...player.finalDiscard]);
		room.baseline = { hands, discardCounts: countTypes(allDiscard, room.selected), handCounts: countTypes(hands.flat(), room.selected), finalByPlayer: room.players.map((player) => [...player.finalDiscard]), publicByPlayer: room.players.map((player) => [...player.publicDiscard]) };
		room.phase = "actions"; room.actionCursor = 0; room.discussionEndsAt = null;
		room.players.forEach((player) => { player.revealed = false; });
		room.log.push("마지막 토론이 끝났습니다. 마지막 손패를 기록하고 직업을 공개합니다.");
		this.advanceAutomaticActions(room);
	}
	private async advanceTurn(room: Room) {
		room.turnCursor += 1;
		if (room.turnCursor < room.turnOrder.length) { this.beginPlayerTurn(room, room.turnOrder[room.turnCursor]); return; }
		await this.beginDiscussion(room);
	}
	private addChat(room: Room, player: Player, payload: unknown) {
		const text = String((payload as { text?: unknown } | null)?.text ?? "").trim().slice(0, 240);
		if (!text) throw new Error("보낼 내용을 입력하세요.");
		room.chat.push({ id: randomId(6), playerIndex: player.index, name: player.name, text, round: room.round, sentAt: Date.now() });
		if (room.chat.length > 120) room.chat.splice(0, room.chat.length - 120);
	}
	private voteToSkipDiscussion(room: Room, player: Player) {
		if (room.discussionVotes.includes(player.id)) {
			room.discussionVotes = room.discussionVotes.filter((id) => id !== player.id);
			room.log.push(`${player.name} 플레이어가 토론 종료 동의를 취소했습니다.`);
			return;
		}
		room.discussionVotes.push(player.id);
		room.log.push(`${player.name} 플레이어가 토론 종료에 동의했습니다. (${room.discussionVotes.length}/${room.players.length})`);
		if (room.discussionVotes.length === room.players.length) {
			room.log.push("전원 동의로 토론을 건너뜁니다.");
			this.finishDiscussion(room);
		}
	}
	private finishDiscussion(room: Room) {
		const completedRound = room.round;
		room.discussionEndsAt = null; room.discussionVotes = [];
		if (completedRound < 4) {
			room.round = completedRound + 1;
			room.log.push(`${room.round}라운드를 시작합니다.`);
			this.beginRound(room);
		} else this.beginActions(room);
	}
	async alarm(): Promise<void> {
		const room = this.requireRoom();
		if (room.phase !== "discussion" || !room.discussionEndsAt) return;
		if (room.discussionEndsAt > Date.now()) { await this.ctx.storage.setAlarm(room.discussionEndsAt); return; }
		this.finishDiscussion(room);
		await this.saveAndBroadcast();
		this.ctx.waitUntil(this.checkBots());
	}
	private advanceAutomaticActions(room: Room) { while (room.actionCursor < ACTION_ORDER.length) { const role = ACTION_ORDER[room.actionCursor]; const player = room.players.find((candidate) => candidate.role === role); if (!player) { room.actionCursor++; continue; } if (role !== "blacksmith") return; player.forged = shuffle(room.players.flatMap((candidate) => candidate.finalDiscard)).slice(0, 3); player.action.blacksmith = { cards: player.forged }; room.actionCursor++; } this.beginAccusation(room); }
	// Everyone names one opponent and their role before the reveal; the
	// assassin names two. This is what makes deduction pay for every role.
	private beginAccusation(room: Room) {
		room.phase = "accusation";
		room.players.forEach((player) => { player.accusations = []; player.accused = false; });
		room.log.push("최종 지목: 상대 한 명과 그 직업을 지목하세요. (암살자는 두 명)");
		this.autoAccuseBots(room);
	}
	private accusationQuota(player: Player) { return player.role === "assassin" ? 2 : 1; }
	private jesterThreshold(room: Room) { return JESTER_THRESHOLD[room.players.length] ?? 3; }
	private autoAccuseBots(room: Room) {
		for (const bot of room.players.filter((p) => p.isBot && !p.accused)) {
			const others = shuffle(room.players.filter((p) => p.index !== bot.index));
			const picks = others.slice(0, this.accusationQuota(bot));
			bot.accusations = picks.map((victim) => ({ player: victim.index, role: shuffle(room.selected)[0] }));
			bot.accused = true;
		}
		this.finishAccusationIfReady(room);
	}
	private finishAccusationIfReady(room: Room) {
		if (room.players.some((player) => !player.accused)) return;
		room.phase = "results";
		room.players.forEach((player) => { player.revealed = true; });
		room.log.push("모든 지목이 끝났습니다. 직업을 공개합니다.");
	}
	private accusationScore(player: Player) {
		const room = this.requireRoom();
		const correct = (player.accusations ?? []).filter((guess) => {
			const victim = room.players.find((p) => p.index === guess.player);
			return !!victim && victim.index !== player.index && victim.role === guess.role;
		}).length;
		const exposedBy = room.players.filter((other) => other.index !== player.index
			&& (other.accusations ?? []).some((guess) => guess.player === player.index && guess.role === player.role)).length;
		return { correct, exposedBy, points: (correct > 0 ? ACCUSE_CORRECT : 0) + exposedBy * ACCUSE_EXPOSED_EACH };
	}
	private currentActionPlayer() { const room = this.requireRoom(); return room.players.find((player) => player.role === ACTION_ORDER[room.actionCursor]); }
	private resolveAction(room: Room, player: Player, input: Record<string, unknown>) {
		const role = ACTION_ORDER[room.actionCursor]; const target = (value: unknown) => room.players.find((candidate) => candidate.index === Number(value)); const roleInput = (value: unknown) => typeof value === "string" && value in ROLE_DATA ? value as RoleKey : null;
		if (role === "assassin") { const guesses = Array.isArray(input.guesses) ? input.guesses : []; if (guesses.length !== 2) throw new Error("추측 대상 2명을 정확히 지정해야 합니다."); const parsed = guesses.map((guess) => ({ player: target((guess as Record<string, unknown>).player), role: roleInput((guess as Record<string, unknown>).role) })); if (parsed.some((guess) => !guess.player || !guess.role || guess.player === player) || parsed[0].player === parsed[1].player) throw new Error("자신이 아닌 서로 다른 플레이어를 지목해야 합니다."); player.action.assassin = parsed.map((guess) => ({ player: guess.player!.index, role: guess.role! })); }
		else if (role === "slave") { const victim = target(input.player); const guessed = roleInput(input.role); if (!victim || victim === player || !guessed) throw new Error("지목할 상대방과 직업을 모두 올바르게 선택해야 합니다."); player.action.slave = { player: victim.index, role: guessed }; }
		else if (role === "hunter") { const victim = target(input.player); const guessed = roleInput(input.role); if (!victim || victim === player || !guessed) throw new Error("자신이 아닌 대상을 지목해야 합니다."); player.action.hunter = { player: victim.index, role: guessed }; }
		else if (role === "chancellor") { const guessed = roleInput(input.role); if (!guessed) throw new Error("예측할 직업을 지정해야 합니다."); player.action.chancellor = { role: guessed }; }
		else if (role === "courtesan") { const victim = target(input.player); if (!victim || victim === player) throw new Error("자신이 아닌 대상을 지목해야 합니다."); player.action.courtesan = { player: victim.index }; }
		else if (role === "thief") {
			// The thief names a player and a slot for each of the two cards. The
			// hand is hidden, so the slot is a blind position, but naming the
			// same player twice is allowed and is the whole point of the role.
			const picks = Array.isArray(input.picks) ? input.picks : [];
			if (picks.length !== 2) throw new Error("훔칠 카드 2장을 지정해야 합니다.");
			const resolved = picks.map((entry) => {
				const record = entry as Record<string, unknown>;
				const victim = target(record.player);
				const index = Number(record.index);
				if (!victim || victim === player) throw new Error("자신이 아닌 대상을 지목해야 합니다.");
				if (!Number.isInteger(index) || index < 0 || index >= victim.hand.length) throw new Error("그 대상에게는 없는 카드 위치입니다.");
				return { victim, card: victim.hand[index] };
			});
			if (resolved[0].card.id === resolved[1].card.id) throw new Error("같은 카드를 두 번 고를 수 없습니다.");
			// Both cards are looked up before anything is removed, otherwise
			// taking the first would shift the second index.
			for (const pick of resolved) pick.victim.hand = pick.victim.hand.filter((candidate) => candidate.id !== pick.card.id);
			const stolen = resolved.map((pick) => pick.card);
			player.hand.push(...stolen);
			player.action.thief = { cards: stolen };
		}
		else if (role === "king") {
			const victim = target(input.player);
			if (!victim || victim === player) throw new Error("자신이 아닌 다른 플레이어를 선택해야 합니다.");
			const indices = Array.isArray(input.indices) ? input.indices : [];
			if (indices.length !== 2) throw new Error("버리게 할 카드 2장을 정확히 선택해야 합니다.");
			const cardsToDiscard = indices.map(idx => victim.hand[Number(idx)]).filter(c => c);
			if (cardsToDiscard.length !== 2 || cardsToDiscard[0].id === cardsToDiscard[1].id) throw new Error("유효하지 않은 카드 선택입니다.");
			victim.hand = victim.hand.filter(c => !cardsToDiscard.includes(c));
			victim.finalDiscard.push(...cardsToDiscard);
			player.action.king = { player: victim.index, cards: cardsToDiscard };
		}
		else if (role === "queen") {
			const type = input.type as string;
			const swapsInput = Array.isArray(input.swaps) ? input.swaps : [];
			const swaps = swapsInput.map(s => {
				const ownId = (s as any).ownId; const graveId = (s as any).graveId;
				let fromGrave, owner;
				if (typeof graveId === "string" && graveId.startsWith("secret-")) {
					const [, playerId, indexStr] = graveId.split("-");
					owner = room.players.find(p => p.id === playerId);
					fromGrave = owner?.finalDiscard[Number(indexStr)];
				} else {
					owner = room.players.find(p => type === "public" ? p.publicDiscard.some(c => c.id === graveId) : p.finalDiscard.some(c => c.id === graveId));
					fromGrave = owner ? (type === "public" ? owner.publicDiscard : owner.finalDiscard).find(c => c.id === graveId) : undefined;
				}
				return {
					own: player.hand.find(c => c.id === ownId),
					fromGrave,
					owner
				};
			});
			if (type === "public") {
				if (swaps.length !== 2 || !swaps[0].own || !swaps[1].own || !swaps[0].fromGrave || !swaps[0].owner) throw new Error("공개 무덤 교환이 유효하지 않습니다.");
				if (swaps[0].own!.id === swaps[1].own!.id) throw new Error("서로 다른 카드를 버려야 합니다.");
				// swap[0] contains the grave info, swap[1] just contains the second card to discard.
				player.hand = player.hand.filter(c => c.id !== swaps[0].own!.id && c.id !== swaps[1].own!.id);
				swaps[0].owner.publicDiscard = swaps[0].owner.publicDiscard.filter(c => c.id !== swaps[0].fromGrave!.id);
				player.finalDiscard.push(swaps[0].own!, swaps[1].own!);
				player.hand.push(swaps[0].fromGrave!);
				player.action.queen = { type: "public", count: 1 };
			} else if (type === "secret") {
				// The secret grave is hidden, so the client cannot name a card in
				// it - it only sends the two cards it is giving up. The server
				// draws the replacements at random, which is what the rule says.
				if (swaps.length !== 2 || !swaps[0].own || !swaps[1].own) throw new Error("버릴 카드 2장을 골라야 합니다.");
				if (swaps[0].own!.id === swaps[1].own!.id) throw new Error("서로 다른 카드를 버려야 합니다.");
				const pool = room.players.flatMap((owner) => owner.finalDiscard.map((card) => ({ owner, card })));
				if (pool.length < 2) throw new Error("비공개 무덤에 가져올 카드가 부족합니다.");
				const drawn = shuffle(pool).slice(0, 2);
				for (let i = 0; i < 2; i++) {
					player.hand = player.hand.filter(c => c.id !== swaps[i].own!.id);
					player.finalDiscard.push(swaps[i].own!);
				}
				for (const pick of drawn) {
					pick.owner.finalDiscard = pick.owner.finalDiscard.filter(c => c.id !== pick.card.id);
					player.hand.push(pick.card);
				}
				player.action.queen = { type: "secret", count: 2 };
			} else { throw new Error("교환 방식을 선택해야 합니다."); }
		}
		else if (role === "mercenary") {
			const group = Array.isArray(input.group) ? [...new Set(input.group.filter((id): id is string => typeof id === "string"))] : [];
			if (player.hand.length < 4 || group.length !== 4 || group.some((id) => !player.hand.some((card) => card.id === id))) throw new Error("교환 전 현재 손패에서 목표 카드 4장을 고르세요.");
			const requestedSwaps = Array.isArray(input.swaps) ? input.swaps : [];
			if (requestedSwaps.length > 2) throw new Error("교환은 최대 2장까지만 가능합니다.");
			const ownIds = new Set<string>(); const graveIds = new Set<string>();
			const swaps = requestedSwaps.map((value) => {
				if (!value || typeof value !== "object") throw new Error("교환 형식이 올바르지 않습니다.");
				const { ownId, graveId } = value as { ownId?: unknown; graveId?: unknown };
				if (typeof ownId !== "string" || typeof graveId !== "string" || ownIds.has(ownId) || graveIds.has(graveId)) throw new Error("중복된 카드이거나 형식이 잘못되었습니다.");
				const own = player.hand.find((card) => card.id === ownId);
				const graveOwner = room.players.find((candidate) => candidate.publicDiscard.some((card) => card.id === graveId));
				const fromGrave = graveOwner?.publicDiscard.find((card) => card.id === graveId);
				if (!own || !graveOwner || !fromGrave) throw new Error("가지고 있지 않은 카드이거나 유효하지 않은 공개 무덤 카드입니다.");
				ownIds.add(ownId); graveIds.add(graveId); return { own, graveOwner, fromGrave };
			});
			for (const swap of swaps) {
				player.hand = player.hand.filter((card) => card.id !== swap.own.id);
				swap.graveOwner.publicDiscard = swap.graveOwner.publicDiscard.filter((card) => card.id !== swap.fromGrave.id);
				player.publicDiscard.push(swap.own);
				player.hand.push(swap.fromGrave);
			}
			const mappedGroup = group.map((id) => swaps.find((swap) => swap.own.id === id)?.fromGrave.id ?? id);
			const cards = player.hand.filter((card) => mappedGroup.includes(card.id));
			if (cards.length !== 4) throw new Error("최종 목표 카드 4장을 가지고 있지 않습니다.");
			player.action.mercenary = { success: new Set(cards.map((card) => card.type)).size === 1, cards, swaps: swaps.map((swap) => ({ gave: swap.own, received: swap.fromGrave })) };
		}
		room.actionCursor++; this.advanceAutomaticActions(room);
	}

	private evaluate(player: Player): Evaluation {
		const room = this.requireRoom(); const role = player.role!; const baseline = room.baseline!; const own = player.hand;
		const minDiscard = Math.min(...Object.values(baseline.discardCounts));
		const handCountsArray = Object.values(baseline.handCounts);
		const maxHands = Math.max(...handCountsArray);
		const totalHands = handCountsArray.reduce((sum, count) => sum + count, 0);
		const count = (type: RoleKey, cards: Card[] = own) => cards.filter((card) => card.type === type).length;
		let success = false; let detail = "";
		switch (role) {
			case "noble": success = count("noble", room.players.flatMap((p) => p.finalDiscard)) === 3; break;
			case "assassin": {
				// Pure identity guess now, resolved from the accusation phase.
				const guesses = player.accusations ?? [];
				success = guesses.length === 2 && guesses.every((guess) => {
					const victim = room.players.find((p) => p.index === guess.player);
					return !!victim && victim.role === guess.role;
				});
				break; }
			case "beggar": success = totalHands < room.players.length * 2; break;
			case "slave": {
				// Must name the right person AND that person must have won.
				const guess = (player.accusations ?? [])[0];
				const victim = guess ? room.players.find((p) => p.index === guess.player) : undefined;
				success = !!victim && victim.role === guess!.role && this.evaluate(victim).success;
				break; }
			case "jester": {
				// The jester wins on attention, not on being misread: either the
				// assassin spends one of their two picks here, or enough of the
				// table names them. Whether the guessed role was right is irrelevant,
				// and only final accusations count.
				const namedBy = room.players.filter((candidate) => candidate.index !== player.index
					&& (candidate.accusations ?? []).some((guess) => guess.player === player.index)).length;
				const assassinNamed = room.players.some((candidate) => candidate.role === "assassin"
					&& (candidate.accusations ?? []).some((guess) => guess.player === player.index));
				success = assassinNamed || namedBy >= this.jesterThreshold(room);
				break; }
			case "priest": success = room.players.every((candidate) => count("priest", candidate.hand) >= 1); break;
			case "knight": success = (baseline.discardCounts.knight || 0) === minDiscard; break;
			case "bard": success = own.length >= 1 && own.every((card) => card.type === "bard") && count("bard", baseline.finalByPlayer[player.index]) >= 1; break;
			case "hunter": { const action = player.action.hunter as { player: number; role: RoleKey } | undefined; success = !!action && count(action.role, baseline.hands[action.player]) >= 1; break; }
			case "commoner": success = (baseline.handCounts.commoner || 0) === maxHands; break;
			case "merchant": success = totalHands > room.players.length * 2; break;
			case "thief": { const cards = (player.action.thief as { cards: Card[] } | undefined)?.cards || []; success = cards.length === 2 && new Set(cards.map((card) => card.type)).size === 1; break; }
			case "mercenary": success = !!(player.action.mercenary as { success?: boolean } | undefined)?.success; break;
			case "seer": success = own.length >= 2 && new Set(own.map((card) => card.type)).size === 1; break;
			case "alchemist": success = [-1, 0, 1].every((score) => own.some((card) => ROLE_DATA[card.type].score === score)); break;
			// Four distinct types, independent of player count: scaling with the
			// table made this near-impossible at 9-10 players.
			case "librarian": success = Object.values(countTypes(own, room.selected)).filter((value) => value >= 1).length >= 4; break;
			case "mage": success = own.length >= 3 && own.reduce((sum, card) => sum + ROLE_DATA[card.type].score, 0) === 0; break;
			case "farmer": success = own.length >= 5 && count("farmer") >= 3; break;
			case "courtesan": {
				// The default argument of count() is the courtesan's own hand, so
				// this used to check "do I hold 2+ of that type" - nothing to do
				// with the card text. Compare against every hand instead.
				const action = player.action.courtesan as { player: number } | undefined;
				const firstType = action ? baseline.hands[action.player][0]?.type : undefined;
				success = !!firstType && (baseline.handCounts[firstType] || 0) === maxHands;
				break; }
			case "pope": success = (baseline.discardCounts.pope || 0) >= 6 && count("pope") >= 1; break;
			case "barbarian": success = player.publicDiscard.length >= 5 && room.players.every((candidate) => candidate.index === player.index || candidate.publicDiscard.length <= player.publicDiscard.length); break;
			case "chancellor": { const action = player.action.chancellor as { role: RoleKey } | undefined; success = !!action && (baseline.handCounts[action.role] || 0) === maxHands; break; }
			case "king": {
				const deployedKingCount = 12 - (room.removed || []).filter((card) => card.type === "king").length;
				const kingAbsentFromFinalHands = deployedKingCount <= 9 && (baseline.handCounts.king || 0) === 0;
				success = (baseline.discardCounts.king || 0) >= 9 || kingAbsentFromFinalHands;
				if (success && kingAbsentFromFinalHands) detail = `투입된 국왕 ${deployedKingCount}장: 마지막 손패에 국왕이 없습니다.`;
				break;
			}
			case "queen": { const ownCount = count("queen"); success = ownCount >= 2 && room.players.every((candidate) => candidate.index === player.index || count("queen", baseline.hands[candidate.index]) < ownCount); break; }
			case "blacksmith": success = false; break;
		}

		let score = player.hand.reduce((sum, card) => sum + ROLE_DATA[card.type].score, 0);
		let base = role === "blacksmith" ? Math.min(12, Math.abs(score + player.forged.reduce((sum, card) => sum + ROLE_DATA[card.type].score, 0)) * 2) : score;
		if (role === "blacksmith") detail = `기본 ${score >= 0 ? "+" : ""}${score}점, 단조 ${player.forged.reduce((sum, card) => sum + ROLE_DATA[card.type].score, 0)}점 = 총 ${base}점`;
		const accuse = this.accusationScore(player);
		const roleBonus = success ? ROLE_DATA[role].bonus : 0;
		if (accuse.correct > 0 || accuse.exposedBy > 0) {
			const parts: string[] = [];
			if (accuse.correct > 0) parts.push(`지목 적중 ${accuse.correct}회 +${ACCUSE_CORRECT}`);
			if (accuse.exposedBy > 0) parts.push(`${accuse.exposedBy}명에게 정체 간파당함 ${accuse.exposedBy * ACCUSE_EXPOSED_EACH}`);
			detail = detail ? `${detail} · ${parts.join(" · ")}` : parts.join(" · ");
		}
		return { success, base, bonus: roleBonus, accusePoints: accuse.points, accuseCorrect: accuse.correct, exposedBy: accuse.exposedBy, total: base + roleBonus + accuse.points, detail };
	}
	private standardSuccess(player: Player, role: RoleKey): boolean { const original = player.role; player.role = role; const result: boolean = this.evaluate(player).success; player.role = original; return result; }
	private viewFor(sessionId: string) {
		const room = this.requireRoom(); const you = this.requirePlayer(sessionId);
		const actionRole = room.phase === "actions" ? ACTION_ORDER[room.actionCursor] : null;
		const actionPlayer = actionRole ? this.currentActionPlayer() : null;
		const publicPlayers = room.players.map((player) => ({ index: player.index, name: player.name, handCount: player.hand.length, selectedCount: player.selectedCount || 0, publicDiscard: player.publicDiscard, finalDiscardCount: player.finalDiscard.length, role: room.phase === "results" || player.revealed ? player.role : null }));
		const results = room.phase === "results" ? room.players.map((player) => ({ index: player.index, name: player.name, role: player.role, hand: player.hand, finalDiscardCount: player.finalDiscard.length, ...this.evaluate(player) })).sort((a, b) => b.total - a.total) : null;
		return {
			roomCode: room.code, phase: room.phase, playerTarget: room.playerTarget, discussionSeconds: room.discussionSeconds, discussionEndsAt: room.discussionEndsAt, discussionVotes: room.players.filter((player) => room.discussionVotes.includes(player.id)).map((player) => player.index),
			selected: room.selected, players: publicPlayers, host: room.hostId === sessionId, round: room.round, turn: room.turn, required: room.required,
			lastRoll: room.lastRoll, chat: room.chat.slice(-120), log: room.log.slice(-6),
			accusedCount: room.players.filter((p) => p.accused).length,
			you: { index: you.index, name: you.name, role: you.role, hand: you.hand, canDiscard: room.phase === "round" && room.players[room.turn]?.id === sessionId, canAct: room.phase === "actions" && actionPlayer?.id === sessionId, canAccuse: room.phase === "accusation" && !you.accused, accuseQuota: this.accusationQuota(you) },
			action: actionPlayer ? { role: actionRole, playerIndex: actionPlayer.id === sessionId ? actionPlayer.index : -1 } : null, results,
			actionPreview: room.actionPreview || {}
		};
	}
	private async stateFor(sessionId?: string) { if (!sessionId || !this.room?.players.some((player) => player.id === sessionId)) return bad("방 참가 정보가 없습니다.", 401); return json({ state: this.viewFor(sessionId) }); }
	private requireRoom() { if (!this.room) throw new Error("방을 찾을 수 없습니다."); return this.room; }
	private requirePlayer(id: string) { const player = this.requireRoom().players.find((candidate) => candidate.id === id); if (!player) throw new Error("이 방의 플레이어가 아닙니다."); return player; }
	private async saveAndBroadcast() { await this.ctx.storage.put("room", this.requireRoom()); for (const socket of this.ctx.getWebSockets()) { const sessionId = socket.deserializeAttachment() as string | null; if (sessionId) socket.send(JSON.stringify({ type: "STATE", state: this.viewFor(sessionId) })); } }
}

export default {
	async fetch(request, env): Promise<Response> {
		const url = new URL(request.url); if (!url.pathname.startsWith("/api/")) return env.ASSETS.fetch(request);
		if (request.method === "POST" && url.pathname === "/api/rooms") { const body = await request.json<{ name?: string; playerTarget?: number }>(); const code = randomId(4).toUpperCase(); const session = randomId(); const stub = env.GAME_ROOM.getByName(`room:${code}`); const response = await stub.fetch("https://room.local/join", { method: "POST", headers: { "content-type": "application/json", "X-Player-Session": session }, body: JSON.stringify({ ...body, code }) }); return withSession(response, session, url.protocol === "https:"); }
		const match = url.pathname.match(/^\/api\/rooms\/([A-F0-9]+)(?:\/(join|state|ws))?$/); if (!match) return bad("없는 API입니다.", 404); const [, code, endpoint = "state"] = match; const session = parseCookie(request, "crown_session") || randomId(); const stub = env.GAME_ROOM.getByName(`room:${code}`); const headers = new Headers({ "X-Player-Session": session }); if (request.headers.get("Upgrade")) headers.set("Upgrade", request.headers.get("Upgrade")!); if (request.headers.get("content-type")) headers.set("content-type", request.headers.get("content-type")!); const response = await stub.fetch(new Request(`https://room.local/${endpoint}`, { method: request.method, headers, body: request.method === "GET" ? undefined : request.body })); return endpoint === "join" ? withSession(response, session, url.protocol === "https:") : response;
	},
} satisfies ExportedHandler<Env>;

function withSession(response: Response, session: string, secure: boolean) { const headers = new Headers(response.headers); headers.append("Set-Cookie", `crown_session=${session}; Path=/; HttpOnly; SameSite=Lax; Max-Age=604800${secure ? "; Secure" : ""}`); return new Response(response.body, { status: response.status, statusText: response.statusText, headers }); }
