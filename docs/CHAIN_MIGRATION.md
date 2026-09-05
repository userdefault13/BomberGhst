# SIM to Base Sepolia: open items

BomberGhst reads cartridges from the SIM backend today and can already read the
console and cartridge diamonds on Base Sepolia (`SOURCE` on the cartridge screen
switches between them). These are the things that behave differently, or not at
all, on the chain path. They are worth settling **before** the cutover, because
each one either needs a contract change or a decision about where the data
comes from.

References below point at `~/Dev/AarcadeGh-t`.

---

## 1. A bound hero has no collateral on chain

**This is the one that changes what the player sees.** The local player wears
whichever Aavegotchi is bound to their cartridge, and the collateral picks the
sprite row. The SIM reports it (`hero.collateral`, a bare symbol, plus
`hauntId`), so on SIM the bomber is the player's own gotchi. On chain there is
nothing to read, so it silently falls back to the slot default and every
player's cartridge looks the same.

What the contracts actually do:

- `IAarcadeCartridge.CAavegotchi` (`contracts/interfaces/IAarcadeCartridge.sol`)
  has no collateral field.
- `_writeHeroFromL1` (`contracts/cartridge/facets/CAavegotchiFacet.sol:120`)
  fetches `IL1Aavegotchi.AavegotchiInfo`, **which does carry
  `address collateral`**, and then drops it when building the hero struct. The
  data is in hand at bind time and simply is not persisted.
- `bindStarter` takes a `collateral` address, forwards it to the fee splitter
  for the Line C split, then discards it (`collateral;` at the end of the
  function). A starter hero has no recorded collateral either.

### Options

| | Approach | Cost | Notes |
|---|---|---|---|
| **A** | Add `address collateral` to `CAavegotchi` and set it in `_writeHeroFromL1` and `bindStarter` | facet upgrade + storage change | Cleanest. The value is already available at both call sites, so it is a few lines plus a diamond cut. Existing heroes stay blank unless backfilled. |
| **B** | Client reads the L1 Aavegotchi diamond for `sourceTokenId` | no contract change | Only works for owned/rental binds; starters have `sourceTokenId == 0`. Needs `l1AavegotchiDiamond`, which is **blank** in `config/cartridgeChain.base-sepolia.json`, plus a second RPC and an extra round trip per cartridge. |
| **C** | Index it off chain in the subgraph | no contract change | `CAavegotchiBound` carries only `bindType` and `sourceTokenId`, so the indexer would still have to resolve L1 itself. Moves the problem rather than solving it. |

**Recommendation: A**, folded into the same upgrade as item 2 below, since both
are changes to the same facet and both are needed before hero data is usable on
chain at all.

---

## 2. There is no getter for a hero

Bigger than the collateral question and blocking it. The cartridge diamond
exposes `getActiveHeroId` and `heroIds`, both of which return `bytes32` ids, and
nothing that returns the hero itself. Traits, level, kinship, bind type and
source token id are all written to storage and then unreadable.

Any client that wants to show a bound gotchi needs something like:

```solidity
function getHero(uint256 cartridgeId, bytes32 heroId)
    external view returns (IAarcadeCartridge.CAavegotchi memory);
```

Adding collateral to the struct (item 1) is only useful once this exists.

---

## 3. Minting is gated to the protocol

`CartridgeFactoryFacet.mintCartridge` requires `msg.sender` to be the protocol
multisig or an authorized minter, so a player wallet cannot mint its own
cartridge; it reverts with `Console: !minter`. This is why minting currently
goes through the SIM, which holds that authority.

Decide before cutover whether the backend keeps minting as an authorized minter
(and the game keeps calling it over HTTP), or whether players mint directly and
the gate is relaxed. The game wires both paths and surfaces the revert rather
than hiding it.

## 4. `bindOwned` needs the L1 diamond address

`bindOwned` and `bindRental` call through `IL1Aavegotchi` to check ownership or
lending. `l1AavegotchiDiamond` is empty in the Base Sepolia config, so both
revert today. `bindStarter` works, at the contract's hardcoded 5 ETH
`STARTER_BIND_FEE`, which is also worth revisiting for a testnet.

## 5. The Line A fee is not readable

`GameRulesFacet.lineAPaid` reports whether a fee is outstanding but nothing
exposes `mintFeeGhst`, so a client cannot know what to send with `payLineA`.
The game reads the amount from `PlayerPrefs("chain.lineAWei")` as a stopgap.
A `lineAFee(uint256 cartridgeId)` view would remove the guesswork. Games with
no mint fee are unaffected, since `lineAPaid` already returns true for them.

## 6. Checkpoints hash differently on each side

Not a blocker, but a checkpoint written on one back end will not verify against
the other:

| | SIM | Chain |
|---|---|---|
| State hash | `sha256` over `JSON.stringify(state, sortedKeys)` | `keccak256` |
| Message | `cartridgeId:` / `nonce:` / `stateHash:` | `Cartridge:` / `Nonce:` / `State:` |
| Auth | owner `personal_sign`, verified server side | `msg.sender` must own the cartridge |

`Assets/Scripts/Cartridges/CheckpointCrypto.cs` implements both and the self
test pins each against its reference. If checkpoint history should carry across
the migration, the two formats need to converge, or the migration needs to
record which scheme each nonce used.

---

## Cutover checklist

- [ ] Add `collateral` to `CAavegotchi`, set it in `_writeHeroFromL1` and `bindStarter` (item 1)
- [ ] Add a `getHero` view (item 2)
- [ ] Decide who mints: backend as authorized minter, or players directly (item 3)
- [ ] Set `l1AavegotchiDiamond` in the chain config, revisit the bind fees (item 4)
- [ ] Add a Line A fee view, drop the `chain.lineAWei` stopgap (item 5)
- [ ] Decide whether checkpoint history migrates, and how (item 6)
- [ ] Flip the default source from `Sim` to `Chain` in `CartridgeService`
