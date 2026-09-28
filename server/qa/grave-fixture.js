// Local-only UI fixture, never included in the deployed assets directory.
state = {
  roomCode: "LOCAL-QA", phase: "round", round: 2, turn: 0, required: null,
  playerTarget: 5, host: true, selected: ["king", "noble", "assassin", "beggar", "priest"],
  players: ["나", "민서", "지우", "서준", "하린"].map((name, index) => ({
    index, name, handCount: 6,
    publicDiscard: index === 1 ? [{ id: "noble-4", type: "noble" }, { id: "assassin-0", type: "assassin" }]
      : index === 2 ? [{ id: "king-0", type: "king" }] : [],
    // Deliberately present to detect an accidental private-card UI leak.
    finalDiscard: [{ id: "queen-11", type: "queen" }],
  })),
  you: { index: 0, role: "noble", canDiscard: true, hand: [
    { id: "assassin-3", type: "assassin" }, { id: "noble-4", type: "noble" },
    { id: "king-2", type: "king" }, { id: "priest-1", type: "priest" },
  ] }, chat: [], discussionVotes: [],
};
const fixtureControls = document.createElement("nav");
fixtureControls.style.cssText = "position:fixed;bottom:4px;left:4px;z-index:120;display:flex;gap:8px";
fixtureControls.innerHTML = '<button id="qa-discussion">토론 화면 테스트</button><button id="qa-update">상태 갱신 테스트</button>';
document.body.append(fixtureControls);
document.getElementById("qa-discussion").onclick = () => {
  state.phase = "discussion";
  state.discussionEndsAt = Date.now() + 120000;
  render();
};
document.getElementById("qa-update").onclick = () => {
  state.players[1].publicDiscard = [{ id: "priest-0", type: "priest" }];
  render();
};
render();
