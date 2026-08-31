# Skill Runtime và Attack Sequence Design

## 1. Mục tiêu

Tài liệu này chốt hướng thiết kế runtime cho hệ thống Skill và luồng Normal Attack Override của prototype hiện tại.

Phạm vi gồm:

- Mỗi Hero có đúng một Passive Skill và một Auto Active Skill cố định từ `HeroDefinition`.
- Mỗi skill có runtime và logic riêng.
- Auto Active Skill tự kích hoạt khi `CanUse` hợp lệ, không cần người chơi nhấn nút.
- Normal Attack Override thay thế một lần normal attack bằng một attack sequence riêng.
- `AttackSequence` điều phối các hit, các hook `BeforeHit`/`AfterHit` và bookkeeping kết thúc đòn đánh.
- Shield, buff/debuff, stun và poison có runtime system riêng.

Ngoài phạm vi hiện tại:

- Skill inventory hoặc skill loadout tự do.
- Hero tag và compatibility validation.
- UI chọn hoặc kích hoạt skill thủ công.
- Save cooldown hoặc trạng thái skill giữa các lần deploy.

## 2. Các thuật ngữ chính

### Skill Definition

ScriptableObject chứa dữ liệu authoring và các giá trị cân bằng của skill. Definition không giữ trạng thái runtime.

Các dữ liệu dùng chung gồm identity, tên, icon, description và loại skill. Mỗi concrete definition bổ sung đúng các field mà skill đó cần, chẳng hạn damage multiplier, duration, hit count hoặc max stack.

Description chỉ dùng để hiển thị. Runtime không đọc hoặc phân tích description để lấy giá trị gameplay.

### Skill Runtime

Object tồn tại trong một lần Hero được deploy. Runtime giữ caster, trạng thái sử dụng và toàn bộ logic hoạt động của skill.

Skill runtime được tạo/reset khi `HeroRuntime.Initialize()` thành công. Trạng thái runtime không nằm trong `HeroCombatState` và không được giữ qua retreat, death hoặc deploy lại.

### Attack Sequence

Một lần thực thi attack hoàn chỉnh. Một sequence có thể gồm một direct hit, nhiều hit, nhiều projectile hoặc một AOE đánh nhiều target.

Sequence là đơn vị dùng để:

- Phân biệt attack thường với normal attack override.
- Xác định primary hit và secondary hit.
- Điều phối `BeforeHit` và `AfterHit`.
- Theo dõi projectile/AOE/hit đã hoàn tất hay despawn.
- Phát tín hiệu khi toàn bộ đòn đánh kết thúc.

## 3. Kiến trúc tổng thể

```text
HeroDefinition
├── Passive Skill Definition
└── Auto Active Skill Definition
             │
             ▼
HeroRuntime.Initialize
├── Passive Skill Runtime
└── Auto Active Skill Runtime
             │
             ▼
CanUse → OnUse → StartCooldown
             │
             ├── Skill tức thời → FinishSkill
             ├── Skill theo thời gian → FinishSkill khi effect kết thúc
             └── Normal Attack Override → FinishSkill khi AttackSequence kết thúc
```

`HeroActionHUD` và `PlayerCombatAction.CastHeroSkill()` không còn tham gia vào skill runtime.

## 4. Class model của Skill

### 4.1. `Skill`

Base runtime của mọi skill.

Trách nhiệm:

- Giữ reference tới owner/caster và definition tương ứng.
- Cung cấp lifecycle chung cho một lần deploy.
- Quy định contract `CanUse` và `OnUse`.
- Cho phép concrete skill tự poll hoặc đăng ký gameplay event phù hợp.
- Cleanup event/reference khi Hero rời combat.

Không chịu trách nhiệm:

- Cooldown.
- Target selection cụ thể.
- Damage, heal, shield, buff hoặc status cụ thể.
- Logic riêng của một skill.

### 4.2. `AutoActiveSkill : Skill`

Base runtime dành cho skill chủ động nhưng tự động kích hoạt.

Trách nhiệm:

- Sở hữu cooldown timer.
- Không cho kích hoạt khi skill đang chạy hoặc cooldown chưa kết thúc.
- Reset cooldown về trạng thái sẵn sàng mỗi lần Hero được deploy/initialize.
- Tự gọi `OnUse` khi `CanUse` hợp lệ.
- Gọi `StartCooldown` sau khi activation bắt đầu thành công.
- Chờ concrete skill gọi `FinishSkill` khi phần thực thi kết thúc.

`StartCooldown` và `FinishSkill` có trách nhiệm độc lập:

- `StartCooldown`: chỉ bắt đầu cooldown.
- `FinishSkill`: chỉ kết thúc lần thực thi hiện tại và giải phóng trạng thái skill/caster action liên quan.

Cooldown và thời gian thực thi skill có thể chạy song song.

### 4.3. Concrete Skill

Mỗi skill tự chứa logic hoạt động của chính nó. Không sử dụng một controller trung tâm với `switch` theo `SkillId`.

Concrete skill chịu trách nhiệm:

- Điều kiện riêng trong `CanUse`.
- Target selection riêng.
- Damage/heal calculation riêng.
- Tạo shield hoặc status runtime.
- Đăng ký các hook attack cần thiết.
- Xác định thời điểm `FinishSkill`.

Passive Skill không cần thêm một base class riêng nếu chưa có hành vi chung ngoài `Skill`.

## 5. Skill lifecycle

### 5.1. Skill tức thời

```text
Ready
→ CanUse
→ OnUse
→ StartCooldown
→ apply effect
→ FinishSkill trong cùng frame
```

### 5.2. Skill theo thời gian

```text
Ready
→ CanUse
→ OnUse
→ StartCooldown
→ runtime effect tự tick
→ effect hoàn tất
→ FinishSkill
```

### 5.3. Skill thay thế normal attack

```text
Ready
→ CanUse
→ OnUse: arm Normal Attack Override
→ StartCooldown
→ normal attack kế tiếp tạo override AttackSequence
→ AttackSequenceFinished
→ FinishSkill
```

Pending override phải được cleanup khi Hero retreat, chết hoặc runtime bị disable. Vì skill runtime được reset khi deploy lại nên pending override và cooldown cũ không được giữ lại.

## 6. Runtime Status và Shield

### 6.1. Status Runtime

Shield, stun, poison và buff/debuff không được hardcode vào `Health` hoặc `HitResult`. Mỗi status có runtime riêng trên target và tự quản lý timer/lifecycle của nó.

`UnitRuntime` sở hữu `UnitStatusRuntime` và là nơi duy nhất điều khiển tick bằng `CombatDeltaTime`. Status runtime không có `Update`, coroutine hoặc tự đọc Unity time.

Mỗi status group được xác định bởi:

```text
StatusKey = StatusId + SourceId
```

- Cùng status và cùng source sẽ refresh/thêm stack vào group hiện có.
- Khác source tạo group độc lập.
- `SourceId` được capture từ runtime source khi apply để status vẫn giữ đúng group nếu source bị destroy.
- Poison stack không cần ID riêng; stack được quản lý bên trong poison group tương ứng.

Quy tắc chung:

- Status có tối đa một stack sẽ refresh duration khi được áp dụng lại.
- Status nhiều stack mặc định cho mỗi stack một duration độc lập.
- Status đặc biệt có thể override quy tắc stack/duration riêng.
- DoT snapshot Attack hoặc effect value tại thời điểm tạo stack.
- Giá trị snapshot được truyền vào status runtime; status không đọc lại Attack của caster trong các tick sau.
- Status phải tự cleanup modifier/event khi hết duration hoặc target rời combat.

Stun có một timer cho mỗi `StatusKey`. Target bị stun khi còn ít nhất một stun group active; nhiều stun source không cộng duration mà chạy timer độc lập.

Poison có stack cap riêng cho mỗi `StatusKey`. Khi group đã đủ stack, poison mới thay thế stack có remaining duration thấp nhất bằng stack mới, gồm attacker và snapshot damage mới.

### 6.2. Shield

Shield có system riêng, không phải timed status mặc định.

Quy tắc đã chốt cho Holy Burst:

- Shield tồn tại cho tới khi bị tiêu hao.
- Shield không có duration.
- Shield từ nhiều lần cast được cộng dồn.

## 7. Normal Attack Override

Normal Attack Override thay thế phần thực thi của một normal attack kế tiếp nhưng vẫn đi qua attack pipeline chung.

Quy tắc:

- Một override, dù có nhiều hit, vẫn là đúng một `AttackSequence`.
- Override sequence không tăng bộ đếm normal attack.
- Multi-hit không được tính thành nhiều normal attack.
- Supplemental effect mặc định chỉ áp dụng vào hit đầu tiên của sequence.
- Hit đầu tiên có index `0` và là primary hit; nếu hit này miss thì primary không chuyển sang hit tiếp theo.
- Các hit còn lại là secondary hit.
- Concrete active skill có thể chủ động cho phép supplemental effect áp dụng lên mọi hit khi thiết kế skill yêu cầu.
- Override logic quyết định số hit, target của từng hit, multiplier, projectile/status và timing giữa các hit.

Ví dụ mặc định:

- Triple Slash gồm ba hit, mỗi hit gây 60% Attack, nhưng không tăng Fourth Strike counter.
- Medusa Shot gồm ba hit, mỗi hit gây 60% Attack.
- Poison Shot chỉ tạo một poison stack từ primary hit của Medusa Shot.
- Một skill tương lai có thể cho cả ba hit kích hoạt on-hit effect vì override logic được phép cấu hình riêng.

## 8. Dữ liệu trong attack pipeline

### 8.1. `AttackExecutionData`

Được tạo khi attack fired đối với projectile/AOE. Nó snapshot các dữ liệu đã biết tại thời điểm bắn như attacker, team, effect value và damage type.

Đây không phải kết quả của một hit.

### 8.2. `HitData`

Được tạo khi một hit chuẩn bị được apply lên Hurtbox thực tế.

Nó chứa:

- Attacker.
- Target Hurtbox.
- Attacker team và target side.
- Damage hoặc heal effect.
- Attack type.
- Raw effect value.
- Damage type.
- Hit position.

Direct attack tạo `HitData` gần như ngay khi attack fired. Projectile chỉ tạo `HitData` khi va chạm. AOE tạo một `HitData` riêng cho mỗi Hurtbox.

`HitData` hiện là immutable data. `BeforeHit` không nên mutate cùng một instance; mỗi bước trả về một snapshot `HitData` mới đã được chỉnh sửa hoặc sequence tạo final `HitData` sau khi đã resolve modifier.

### 8.3. `HitResult`

Kết quả sau khi `HitProcessor` xử lý `HitData`.

Nó cho biết:

- Damage/heal thực tế đã áp dụng.
- Target có bị giết bởi hit hay không.

`HitResult` không tự áp status hoặc chạy skill logic.

### 8.4. Resolved hit

Một hit đã resolve được biểu diễn bằng cặp:

```text
HitData + HitResult
```

Không cần copy target sang result khác vì target đã có trong `HitData`.

## 9. Attack Sequence pipeline

Attack pipeline chỉ có hai extension point cho gameplay logic:

```text
HitData ban đầu
→ BeforeHit
→ HitProcessor.TryProcessHit
→ AfterHit nếu effect được apply thành công
```

### 9.1. `BeforeHit`

Dùng cho logic cần chạy trước damage/heal:

- Sửa raw effect value.
- Sửa multiplier.
- Sửa damage type.
- Áp bonus damage cho primary hit.
- Xử lý điều kiện dựa trên toàn bộ target của sequence.

Ví dụ:

- Fourth Strike thêm bonus Physical Damage vào primary hit của normal attack hợp lệ.
- Magic Surge tăng Magic Damage nếu attack có ít nhất ba target duy nhất.
- Triple Slash và Medusa Shot đặt multiplier riêng cho từng hit.

### 9.2. `HitProcessor`

Không phải extension point. Nó chỉ:

- Validate target/team/attack rule.
- Chuyển `HitData` thành `DamageRequest` hoặc `HealRequest`.
- Apply damage/heal nền tảng.
- Trả `HitResult`.

`HitProcessor` không biết concrete skill, poison, stun hoặc buff.

### 9.3. `AfterHit`

Chỉ chạy khi `HitProcessor` xác nhận effect đã được áp dụng thành công.

Dùng cho:

- Thêm poison stack.
- Apply stun.
- Apply buff sau heal.
- On-kill hoặc on-success effect.

Ví dụ:

- Poison Shot tạo poison status từ snapshot value của primary hit.
- Medusa Shot apply stun sau primary hit thành công.
- Protective Mist apply defensive buff khi Djinn thực sự heal target.

### 9.4. Bookkeeping nội bộ

Không có public gameplay phase `ProcessHit` hoặc `CompleteHit`.

Sequence chỉ cần bookkeeping nội bộ để biết:

- Direct hit đã resolve.
- Projectile đã hit hoặc despawn/miss.
- AOE đã despawn.
- Một hit trong multi-hit đã resolve hoặc miss.
- Tất cả carrier/hit của sequence đã hoàn tất.

Khi tất cả hoàn tất, sequence phát `AttackSequenceFinished`. Signal này có thể được Auto Active Skill dùng để gọi `FinishSkill`.

### 9.5. Async attack

Projectile và AOE phải callback về sequence:

- Projectile báo hit khi `HitProcessor` hoàn tất.
- Projectile báo miss/finished khi despawn mà chưa hit.
- AOE báo từng resolved hit.
- AOE báo finished khi runtime despawn.

`NormalAttackController.OnAttack` hiện tại diễn ra lúc attack được khởi tạo nên về nghĩa phù hợp với `AttackFired`, không phải `HitResolved`.

## 10. Target collection cho AOE

Effect cần biết tổng target trước damage, như Magic Surge, không thể xử lý đúng nếu AOE apply damage ngay khi từng collider đi vào.

Luồng cần thiết:

```text
Collect valid targets
→ xác định số target duy nhất
→ chuẩn bị sequence-level condition
→ BeforeHit cho từng target
→ apply từng HitData
```

AOE cố định tại vị trí tạo nếu description không nói rõ rằng nó follow target.

Periodic AOE ghi “mỗi giây” nhưng không nói tick ngay lập tức sẽ tick sau mỗi giây hoàn chỉnh.

## 11. Quy tắc gameplay chung đã chốt

- Cụm “hit 3 times, deal 60% damage” nghĩa là mỗi hit gây 60%, tổng tiềm năng 180%.
- Không diễn giải multi-hit multiplier thành tổng damage chia cho số hit nếu description không nói vậy.
- Mỗi status stack có duration riêng trừ khi status đặc biệt quy định khác.
- Buff một stack refresh duration khi được apply lại.
- DoT snapshot effect value khi tạo stack.
- AOE không nói follow target thì đứng cố định.
- Periodic effect “mỗi giây” không có chỉ dẫn tick đầu thì tick tại giây 1, không tick tại giây 0.
- Supplemental effect của multi-hit mặc định chỉ áp dụng trên primary hit.
- Concrete skill được phép override quy tắc supplemental effect để tạo thiết kế đặc biệt.

## 12. Skill catalogue và dữ liệu riêng

### SK01 — Holy Burst

Loại: Auto Active, cooldown 11 giây.

Hành vi:

- Gây 120% Attack dưới dạng Physical Damage lên enemy trong vùng.
- Tạo shield bằng 15% Max Health cho caster.
- Shield vĩnh viễn cho tới khi bị tiêu hao và cộng dồn qua nhiều lần cast.

Field riêng:

- Damage multiplier.
- Shield Max Health multiplier.
- Area pattern/range.

### SK02 — Iron Guard

Loại: Passive.

Hành vi:

- Khi block ít nhất một enemy, tăng 20% Defense và 20% Special Defense.
- Gỡ modifier khi không còn block enemy.
- Không apply lặp modifier mỗi frame.

Field riêng:

- Minimum blocked enemy count.
- Defense bonus.
- Special Defense bonus.

### SK03 — Triple Slash

Loại: Auto Active, cooldown 8 giây.

Hành vi:

- Arm override cho normal attack kế tiếp.
- Override gồm ba hit.
- Mỗi hit gây 60% Attack dưới dạng Physical Damage.
- Sequence không tăng normal attack counter.
- Supplemental effect mặc định chỉ áp dụng trên hit đầu.

Field riêng:

- Hit count.
- Damage multiplier per hit.
- Timing giữa các hit nếu cần.

### SK04 — Fourth Strike

Loại: Passive.

Hành vi:

- Mỗi normal attack thường thứ tư gây thêm 60% Attack dưới dạng Physical Damage.
- Normal attack override không tăng counter.
- Bonus áp dụng lên primary hit của attack thứ tư.

Field riêng:

- Required normal attack count.
- Bonus damage multiplier.

### SK05 — Medusa Shot

Loại: Auto Active, cooldown 9 giây.

Hành vi:

- Arm override cho normal attack kế tiếp.
- Bắn ba projectile/hit.
- Mỗi hit gây 60% Attack dưới dạng Physical Damage.
- Primary hit apply stun một giây khi hit thành công.
- Sequence không tăng normal attack counter.

Field riêng:

- Projectile/hit count.
- Damage multiplier per hit.
- Stun duration.
- Projectile/VFX reference riêng nếu cần.

### SK06 — Poison Shot

Loại: Passive.

Hành vi:

- Normal attack thành công apply poison lên target.
- Poison tồn tại bốn giây.
- Mỗi giây gây 10% Attack dưới dạng Magic Damage.
- Tối đa ba stack.
- Mỗi stack có duration độc lập.
- Attack/effect value được snapshot khi stack được tạo.
- Trên Medusa Shot, mặc định chỉ primary hit tạo một poison stack.

Field riêng:

- Poison duration.
- Tick interval.
- Damage multiplier per tick.
- Max stack count.

### SK07 — Magic Storm

Loại: Auto Active, cooldown 13 giây.

Hành vi:

- Dừng normal attack trong khi skill thực thi.
- Tạo một storm cố định tại vị trí current target.
- Storm tồn tại bốn giây.
- Gây 50% Attack dưới dạng Magic Damage tại giây 1, 2, 3 và 4.
- Không tick tại giây 0.
- `FinishSkill` sau tick ở giây thứ 4.

Field riêng:

- Duration.
- Tick interval.
- Damage multiplier per tick.
- Area pattern/radius.
- AOE/VFX reference.

### SK08 — Magic Surge

Loại: Passive.

Hành vi:

- Normal attack gây thêm 20% Magic Damage nếu sequence đánh ít nhất ba enemy duy nhất.
- Điều kiện target count phải được xác định trước khi apply hit đầu tiên.

Field riêng:

- Minimum unique target count.
- Bonus Magic Damage multiplier.

### SK09 — Battle Wish

Loại: Auto Active, cooldown 12 giây.

Hành vi:

- Arm override cho normal heal kế tiếp.
- Heal tất cả ally trong range, mỗi ally nhận heal bằng 120% Attack.
- Tất cả ally trong range nhận +15% Attack trong bốn giây, kể cả ally đang đầy máu.
- Attack buff là status một stack; apply lại sẽ refresh duration.

Field riêng:

- Heal multiplier.
- Attack bonus.
- Buff duration.
- Area pattern/range.

### SK10 — Protective Mist

Loại: Passive.

Hành vi:

- Hero thực sự được Djinn heal nhận +12% Defense và +12% Special Defense trong ba giây.
- Buff có tối đa một stack.
- Heal tiếp theo từ Djinn refresh duration.
- Target đầy máu và không nhận heal thực tế sẽ không kích hoạt Protective Mist.
- Nguồn healer được xác định từ resolved hit của chính Djinn, không dựa vào event chỉ có heal amount trên target.

Field riêng:

- Defense bonus.
- Special Defense bonus.
- Buff duration.

## 13. Dữ liệu asset cần điều chỉnh

Các asset skill hiện đang dùng cùng `SkillDefinition` và các con số gameplay chủ yếu chỉ nằm trong description.

Hướng migration:

- Giữ common identity/UI data trong base definition.
- Chuyển cooldown sang definition dành cho Auto Active Skill hoặc concrete Auto Active definition.
- Tạo concrete definition với field riêng cho từng skill.
- Giữ runtime state ngoài ScriptableObject asset.
- Không hardcode các giá trị balance trong runtime nếu chúng cần chỉnh bằng Inspector.

`SkillTargetType` hiện không biểu diễn đầy đủ các skill hỗn hợp. Concrete logic mới là nguồn quyết định target thực tế; enum chỉ nên dùng làm metadata/UI nếu tiếp tục giữ lại.

Các target metadata cần xem lại:

- Iron Guard: Self.
- Battle Wish: Ally/Area.
- Protective Mist: Ally.
- Holy Burst: Enemy + Self.
- Magic Storm: Enemy anchor + Area effect.

## 14. Nền tảng hiện có và phần cần bổ sung

Đã có:

- `CountdownTimer` cho cooldown/duration.
- `HeroBlocker.CurrentBlockCount` cho Iron Guard.
- `UnitStats.AddModifier/RemoveModifier` cho buff/debuff.
- `TargetScanner` cho enemy/ally scan theo pattern.
- `HitProcessor`, `HitData` và `HitResult` cho damage/heal cơ bản.

Cần bổ sung:

- Skill runtime creation và lifecycle trong `HeroRuntime`.
- Auto Active cooldown/use state.
- Normal Attack Override owner/consumption.
- `AttackSequence` và primary/secondary hit metadata.
- Callback từ projectile/AOE về sequence.
- `BeforeHit`/`AfterHit` registration và dispatch.
- Runtime status container.
- Poison, stun và timed stat modifier runtime.
- Shield system.
- Target collection trước damage cho AOE cần sequence-level conditions.

## 15. Thứ tự triển khai đề xuất

1. Runtime Status container và quy tắc duration/stack/snapshot.
2. Stun và Poison runtime dùng chung cho mọi `UnitRuntime`.
3. Base `Skill`, `AutoActiveSkill` và concrete definition factory/lifecycle.
4. Tích hợp hai skill runtime vào `HeroRuntime`; loại skill khỏi `HeroActionHUD` flow.
5. `AttackSequence`, `BeforeHit`, `AfterHit` và bookkeeping kết thúc.
6. Normal Attack Override và callback cho direct/projectile/AOE.
7. Shield system.
8. Triển khai skill theo vertical slice, bắt đầu bằng Iron Guard để kiểm chứng passive lifecycle và modifier cleanup.
9. Triển khai một Auto Active tức thời, sau đó một override multi-hit và cuối cùng periodic AOE/status interactions.

## 16. Các contract chi tiết còn cần chốt khi code từng slice

- Cách concrete definition tạo đúng concrete runtime mà không dùng central `switch`.
- Kiểu dữ liệu cụ thể cho sequence metadata và resolved hit.
- Cách chain nhiều `BeforeHit` theo thứ tự ổn định.
- Cách đăng ký supplemental effect mặc định primary-only và opt-in all-hit.
- Callback contract để projectile/AOE report hit, miss và despawn.
- Quy tắc cancel sequence khi caster chết hoặc rời combat.
- Exact target area/pattern cho các description đang dùng từ “nearby” hoặc “in range”.
- VFX/animation timing của từng multi-hit skill.

Các contract trên phải được thiết kế theo từng vertical slice, giữ class đơn giản và chỉ bổ sung abstraction khi một failure mode hoặc invariant thực tế yêu cầu.
