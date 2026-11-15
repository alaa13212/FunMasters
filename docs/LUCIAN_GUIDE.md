# The Lucian Galade Style Guide

> Voice, tone, and terminology reference for anyone writing as **Lucian Galade, Chief of Staff** of the Council of Fun Masters.

Lucian is the in-character spokesperson for the Council of Fun Masters ("Play or Die") inside the Telegram channel. Every notification, digest, and reminder is delivered in his voice, never first-person casual chatter. This guide locks that voice down so future messages stay consistent and the project stays canon.

Source of truth: [`FunMasters/FunMasters/Services/LucianGalade.cs`](../FunMasters/FunMasters/Services/LucianGalade.cs). If this guide and the code disagree, the code wins — but please update this guide in the same PR.

---

## 1. TL;DR for the impatient

- Lucian speaks in two forms (§4): **full messages** and **one-liners**.
- Open every full message with: `Esteemed members of the <b>Council of Fun Masters</b>,\n`
- Close every full message with: `\n<i>— Lucian Galade, Chief of Staff</i>`
- A one-liner is a single line with no opener, ending in `<i>L.G.</i>`.
- Speak **for** the Council in the third person; first-person only when reporting his own duty ("It is my duty to inform you...").
- One-line paragraphs. Short sentences. No slang. Archaic register: *deliberation, verdict, gavel, docket, duly noted.*
- Dry wit is welcome. Throat-clearing ellipses (`...interest...`) are a signature weapon. Caps are banned — use `<b>...</b>` for emphasis.
- Always escape user input through the existing `Escape()` helper. Telegram renders HTML.

---

## 2. Who is Lucian Galade?

| Trait | Detail |
| --- | --- |
| Title | **Chief of Staff** to the Council of Fun Masters |
| Tone | Ceremonial, formal, wry, slightly ominous |
| Mode | Telegram bot (HTML formatting) |
| Persona | A senior administrator reading the minutes out loud — never the protagonist, always the messenger |
| Lampshade | References to *Anton Rayne* (Disco Elysium) on repeat / escalated reminders — jokes that the Council's displeasure may escalate into a "you are the one having an episode" intervention |

He is **not** a chatty mascot, **not** a teacher, and **not** an admin support bot. When in doubt, ask: *"Would a long-suffering bureaucrat in a velvet-lined council chamber say this, while slightly resenting the entire room?"*

---

## 3. Voice and tone

### 3.1 Register

- **Archaic-formal.** Favor: *deliberation, verdict, gavel, docket, pending, duly noted, ascendant, ascending, observe, considers, recommends, thus, hereby.*
- **Never write:** lol, lmao, btw, hi, hey, just, gonna, wanna, kinda, stuff, things, OK, hey guys.
- **Pronouns:** "the Council" in the third person is the default. The Council is treated as a singular deliberative body — *the Council observes*, *the Council grows impatient.*
- **Self-reference:** Lucian is only mentioned by name in his signature. In-body, switch to first-person *only* for duty statements: *"It is my duty to inform you that..."*

### 3.2 Calibration matrix

| Situation | Register | Sample tone |
| --- | --- | --- |
| New member joined | Warm but measured | *"May their suggestions be ever inspired and their playtime plentiful."* |
| New suggestion | Neutral / procedural | *"The docket has been updated."* |
| Playtime shaming (first) | Dry, pointed | *"The Council notes with... interest..."* |
| Playtime shaming (repeat) | One notch ominous | Add the Anton Rayne line |
| Discount / sale event | Urgent but polished | *"The Council would do well to act swiftly."* |
| Discount expired | Regret-laden | *"The Council may wish they had acted sooner."* |
| Rating reminder | Patient, then strict | *"The Council grows impatient."* |
| Rating complete | Definitive | *"The record stands."* |
| Milestone reached | Mild, approving | *"Duly noted."* |
| Finish early | Executive | *"By order of the Chair..."* |
| Quarterly / seasonal | Ceremonial | *"As the season turns, the Council's records have been reviewed."* |

### 3.3 Things to avoid

- **No exclamation marks.** Lucian does not exclaim. He *observes.*
- **No emoji.** One exception: the long-review announcement invites a gem with 💎, at the Council's request. Do not add others.
- **No all-caps.** Use `<b>` for emphasis.
- **No second-person collective** ("you all", "everyone"). Use *the Council* or *the members.*
- **No breaking character** to mention "Telegram", "bot", "server", "notification", "cron", or any system/infra term. The channel is invisible to Lucian — words like "Discord mod", "admin tool" must never leak.
- **No apologies, no hedging.** If something is wrong, frame it as an executive decision.

---

## 4. Anatomy of a message

```
Esteemed members of the <b>Council of Fun Masters</b>,\n
<one-line blank-or-context lead>\n
<body — short paragraphs, one sentence per line>
  ▪ Bullet lines, two-space indent + ▪ (U+25AA BLACK SMALL SQUARE)
  ▪ Never "- " or "* " bullets.\n
<body closer — single sentence, signature tone>\n
<i>— Lucian Galade, Chief of Staff</i>
```

Every message is one of two forms. Which form a trigger uses is set in `LucianTriggers.All` (`FunMasters.Shared/LucianTriggers.cs`) and shown on the Cabinet page.

**Full message**: the anatomy above, for anything with a list, a ceremony, or weight.

**One-liner**: one sentence (or two short ones) on a single line, then the sign-off. No opener, no signature.

```
<b>Ali</b> has revised their verdict on <b>Hades</b> from <b>6.0</b> to <b>8.5</b>. Duly noted. <i>L.G.</i>
```

A one-liner may carry a link on a second line (the long-review announcement does). Build one with the `OneLine()` helper, never by hand.

### 4.1 Formatting rules

- **Bold with `<b>...</b>`** — names, titles, counts, dates, scores, percentages.
- **Italic with `<i>...</i>`** — verdict labels, the signature, ironic phrases. Italics are reserved for tone, not emphasis.
- **User-supplied strings** must go through `Escape()` before any HTML wrapping. Existing `Escape()` replaces `&`, `<`, `>` only; it does not strip newlines or trim. If you add a new helper, follow the same principle: HTML-safe, no Markdown assumed.
- **Bullets**: two-space indent, then `▪`. Not `•`, not `-`, not numbered.
- **Numbers**: spell out *one* in prose when used as "one member", but keep *1* in counts like `1 day`, `5 verdict(s)`.
- **Plurals**: use singular with `(s)` for general grammaticality: `"member(s)"`, `"verdict(s)"`, `"day(s)"`. It is the established pattern, do not "fix" it.
- **Dates**: `dd MMM yyyy` format. (`15 Jul 2026`).
- **Playtime formatting**: use the existing `FormatPlaytime(int minutes)` helper. Do not hand-format.

---

## 5. Recurring formulae

These are the phrases the existing code uses. Prefer them over invented alternatives; if you invent a new one, add it here.

### 5.1 Mandatory opener
```text
Esteemed members of the <b>Council of Fun Masters</b>,\n\n
```
*(Every builder opens through the `Open()` helper, so the opener has a single source. The old `of <b>The Council of Fun Masters</b>` variant in `BuildDiscountMessage` is gone.)*

### 5.2 Mandatory signature
```text
\n<i>— Lucian Galade, Chief of Staff</i>
```
Held in the `LucianGalade.Signature` constant; full messages close through the `Close()` helper. One-liners end in `LucianGalade.SignOff` (`<i>L.G.</i>`) instead.

### 5.3 Body closers (pick one, or close with a custom one in the same register)

| Closer | Use when |
| --- | --- |
| *The Council awaits.* | Rating reminder |
| *Duly noted.* | Milestone reached |
| *The record stands.* | Rating consensus finalized |
| *The gavel falls.* | Game rotation / handoff |
| *May your deliberations be thorough.* | New game entering deliberation |
| *A seat has been prepared.* | New member welcomed |
| *The docket has been updated.* | Suggestion lifecycle |
| *The Council is advised to stay current.* | Weekly digest |
| *The Council's legacy endures.* | Quarterly / season-wrap |
| *The Council would do well to act swiftly.* | Discount / free-game event |
| *The Council may wish they had acted sooner.* | Discount expired |
| *The Council adjusts its schedule accordingly.* | Finish-early notice |
| *The Council grows impatient.* | Escalated rating reminder |
| *The Council awaits.* | Verdicts-and-gems reminder (day 5 after a title concludes) |
| *The record stands.* | Writer of a title named |
| *The Council's legacy endures.* | Writer of the Cycle named |
| *Duly noted.* | Gem roundup; verdict revised; playtime milestone (one-liner) |
| *Those who find it worthy may award a gem 💎.* | Long review (one-liner) |
| *The docket has been updated.* | Suggestion amended or withdrawn (one-liners) |
| *The Council may wish they had acted sooner.* | Discount ended (one-liner) |
| *As the season turns...* | Quarterly opener |

### 5.4 Signature phrases
- **"The Council notes with... interest..."** — used only for playtime-shaming or other gentle-but-pointed call-outs. The `...` is deliberate. Do not stretch it to "with... disappointment...".
- **"It is my duty to inform you that..."** — used in formal announcements, particularly price events.
- **"By order of the Chair..."** — used when an admin acts unilaterally (e.g., finish early).
- **"At the request of <b>name</b>:"**: frames a member's custom alert from the Cabinet. The member's text follows, escaped.
- **"<b>[!] Urgent notice</b>"**: the first line of an alert its sender marked critical.
- **"The Council would hate for Anton Rayne to take an interest in this matter."** — escalation line, appended once at most, never duplicated in the same message, and reserved for repeat reminders (rating + playtime).

### 5.5 Number callouts

Common numeric phrasings:
- *"Only **5 day(s)** remain in the deliberation period."*
- *"**3** member(s) have yet to deliver their verdict."*
- *"**8.4/10** — <i>Consensus reached</i>"*

Always bold the number. Whenever a verdict label exists, render it italic.

---

## 6. Glossary of system terms

This is the canonical mapping between the **technical entities** (database fields, enums, statuses) and the **in-character terms** Lucian uses. Update both sides when one changes.

### 6.1 The Council and the organization

| Technical | Lucian term(s) |
| --- | --- |
| Project (`FunMasters` repository, `Play or Die`) | **the Council**, **the Council of Fun Masters**, **Play or Die** |
| Telegram group chat | *(never named explicitly)* |
| LucianGalade service | *(never named; speak as Lucian)* |
| Admin (system role) | **the Chair** (singular authority figure when one person is acting) |

### 6.2 Member states (matches `CouncilStatus` enum)

| Enum value | Lucian term |
| --- | --- |
| `Active` | **sitting Fun Master**, **member of the Council** |
| `Candidate` | **aspirant**, **applicant** (in onboarding). *Never call a Candidate a member.* |
| `Excommunicated` | **excommunicated member**, **excommunicated individual**, *the Mark of Excommunication* (for the historical title) |
| `Executed` | **executed individual**, *the Mark of Execution* |
| `Shadow` | **shadow observer**, **the Shadow** (rare in messages; reserved for read-only spectators) |

The Council is plural and gender-neutral. Individual members are referred to by name (bolded, escaped) or as `member(s)`.

### 6.3 Game lifecycle (matches `SuggestionStatus` enum)

| Enum value | Lucian term |
| --- | --- |
| `Pending` | **submitted for the Council's consideration**, **awaiting consideration** |
| `Queued` | **on the docket**, **the next title before the Council**, **up next** |
| `Active` | **Currently in Deliberation**, **before the Council**, **in the deliberation period** |
| `Finished` | **concluded**, **deliberation complete**, **the Council has reached consensus** |

### 6.4 Per-event terminology

| Concept | Lucian term |
| --- | --- |
| A game | **title** (never "game" if you can help it; "title" matches the constitutional register) |
| Suggesting a game | **submit for the Council's consideration** |
| Amending a suggestion | **amend their proposal** |
| Withdrawing a suggestion | **withdraw a proposal from the Council's consideration** |
| Playing the game | **Play** (cap when used as the constitutional verb; lowercase for general usage) |
| Playtime | **dedication**, **investment**, **Playtime standings** |
| Top player | **Most Dedicated** |
| Two-week deliberation cycle | **the deliberation period**, **two (2) weeks** |
| Early finish | **commence early / Play early** |
| Average rating | **consensus**, **average verdict** |
| A single rating (score + review) | **verdict** (one rating = one verdict) |
| Review text | **review** (distinguish from "verdict" — verdict = pair, review = prose) |
| Short / lazy review (<3 words) | **a verdict that lacks the substance befitting a Fun Master's review** |
| Score 1–10 | **numerical score**, **verdict** |
| Rating label / tier | *italic label* (e.g., *Triumphant*, *Controversial* — example pattern) |
| Pending rating | **unfinished rating**, **verdict not yet delivered** |
| Unrated member | **members yet to render judgment** |
| Steam discount | **marked down**, **discount on `<title>` has ended** |
| Steam free event | **a rare occurrence: `<title>` is now <b>FREE</b>** |
| Founder of a Telegram group | implicit — never mentioned |
| Gem (`Gem` entity) | **gem**; *award a gem*, *received a gem*. Gems are anonymous: name the receiver, never the giver |
| Unspent gem | *yet to award their gem*, *still holds a gem for `<title>`* |
| Top-gemmed review on a title | **Writer of `<title>`** (named 7 days after the title concludes) |
| Top-gemmed member across a cycle | **Writer of the Cycle** |
| Review of 150+ words | *a review of <b>N words</b>* (announced once, as a one-liner) |
| Score changed by a full point | *has revised their verdict* |
| Member's custom alert | *At the request of <b>name</b>* |
| A trigger switch / the Cabinet | *(never named; a switched-off trigger is simply silent)* |

### 6.5 Cadence / digest vocabulary

| Cadence | Terminology |
| --- | --- |
| Weekly digest | **the Council's weekly briefing** |
| Quarterly digest | **As the season turns, the Council's records have been reviewed** |
| One-off milestone | **Duly noted.** |
| Morning notification queue | (never named; messages simply arrive in the morning) |

---

## 7. Worked examples

### 7.1 Good — rating reminder, first instance
```text
Esteemed members of the <b>Council of Fun Masters</b>,

It has been <b>14 days</b> since the Council concluded its deliberation of <b>Elden Ring</b>.
<b>3</b> member(s) have yet to deliver their verdict. The Council grows impatient.

Those yet to render judgment:
  ▪ alice
  ▪ bob
  ▪ carol

The Council awaits.
— Lucian Galade, Chief of Staff
```

### 7.2 Good — discount event
```text
Esteemed members of the <b>Council of Fun Masters</b>,

It is my duty to inform you that <b>Outer Wilds</b>, a title currently awaiting the Council's
deliberation, has been marked down on Steam.

<s>$24.99</s> → <b>$9.99</b>  (-60%)
https://store.steampowered.com/app/753640

The Council would do well to act swiftly.
— Lucian Galade, Chief of Staff
```

### 7.3 Good — shaming, repeat, with escalation
```text
Esteemed members of the <b>Council of Fun Masters</b>,

The Council notes with... interest... that the following members have yet to begin <b>Baldur's Gate 3</b>:
  ▪ dave
  ▪ erin

Only <b>2 day(s)</b> remain in the deliberation period.

The Council would hate for Anton Rayne to take an interest in this matter.
— Lucian Galade, Chief of Staff
```

### 7.4 Bad — off-brand, do NOT write

```text
Hi everyone 👋!! Just a quick reminder that some of you haven't rated Outer Wilds yet.
The deadline is in 3 days so pls go do that ASAP lol. Thanks!!
- Fun Masters Bot 🤖
```

Why this is wrong:
- Exclamation marks and emoji are banned.
- "Hi everyone", "you", "lol" — break the third-person Council register.
- No HTML bold, no signature, no opener.
- "Fun Masters Bot" — Lucian is the Chief of Staff, not a bot.
- "pls" and "ASAP" are slang.
- The signature line is plain text, not italic, and gets his title wrong.

### 7.5 Bad — tone slips

```text
Esteemed members of the <b>Council of Fun Masters</b>,

Sorry for the spam :) We've had a tiny issue with the database and the last message was duplicated.
We'll fix it shortly. Apologies again!
— Lucian Galade, Chief of Staff
```

Why this is wrong:
- Lucian does not apologize. If anything went wrong with the system, he describes it as an editorial correction or a procedural note.
- Emoticon and "spam" are out-of-character.

### 7.6 Correct fix of the above

```text
Esteemed members of the <b>Council of Fun Masters</b>,

The Council's previous broadcast was issued in duplicate; the second has been struck from the record.
Duly noted.

— Lucian Galade, Chief of Staff
```

---

## 8. Triggers, switches and the Cabinet

Every message belongs to a **trigger**: an entry in `LucianTriggers.All` with a key, a name, a plain description of when it fires, and its form. The Cabinet page (`/cabinet`, Extras menu, signed-in members) lists them all with a sample message each, and admins can switch any trigger off there. A switched-off trigger is silent: `SendAsync(key, message)` and `QueueForMorningAsync(key, message)` check the switch before anything is sent or queued. A trigger with no stored switch is on.

Each message has a `static Build*Message` method that only formats, and a `Send*Async` method that sends it under its trigger key. The Cabinet's samples (`LucianSamples`) call the same `Build*` methods with fixed sample data, so the page always shows what Lucian really says.

The Cabinet also lets any member send a **custom alert** through Lucian (`BuildAlertMessage`), except those the Council has cast out (`CouncilStatusRoles.CastOut`: excommunicated and executed members), who are refused. A single line of up to 200 characters goes out as a one-liner; anything longer gets the full frame. Alerts are not triggers and have no switch. They replace the old admin "Send Telegram" page.

## 9. Remarks on the website

Lucian also speaks on the site, through `LucianRemarks` (plain text, no HTML, no opener or sign-off):

- **Member pages**: missed verdicts, verdicts under three words, and gems still held on titles whose cycle has not settled. Shown prominently: the member page is where the Council punishes.
- **Game pages**: members yet to deliver a verdict and members yet to award their gem, shown only once the title's Writer has been named. Short reviews are left to the member pages.
- Nothing at all when the record is clean.

The review form's live word counter speaks in the same voice and counts words by `ReviewRules.CountWords`, the rule the server uses: *"112 words. 38 more and Lucian takes notice."*, and a 🚨 warning under three words (the website is not bound by the Telegram no-emoji rule).

## 10. Implementation checklist for new notifications

When adding a new message to `LucianGalade.cs`:

- [ ] Add the trigger to `LucianTriggers.All`: key, name, when it fires, and form (full or one-liner).
- [ ] Write a `static Build*Message` method, and a `Send*Async` that sends it through `SendAsync(key, ...)`.
- [ ] Add a sample for it in `LucianSamples` so the Cabinet can show it.
- [ ] Full messages open through `Open()` and close through `Close()`; one-liners go through `OneLine()`.
- [ ] Use `Escape()` for every user-supplied string and game title; HTML-wrap only the static literals.
- [ ] Bold counts, names (when introduced), scores, and dates.
- [ ] Italic the signature and any verdict labels.
- [ ] End with a body closer picked from §5.3, or add a new one (and document it here).
- [ ] If you introduce a new system status, term, or phrase, update §6 *and* this checklist.

---

## 11. Maintenance

- **Owner**: anyone editing `LucianGalade.cs`. The guide lives in `/docs/LUCIAN_GUIDE.md`; update both at once.
- **Conflicts**: source-of-truth ranking is `LucianGalade.cs` > this guide. When resolving, change the guide to match.
- **Versioning**: when the Constitution (`/constitution`) is amended in a way that renames a role, status, or process, propagate the new term here in the same PR.
- **Adding a notification**: open a PR with both the `Send*Async` method and the corresponding row in §6.4 + §5.3.
