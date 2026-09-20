using System.Collections;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    private static readonly int LastMoveYHash = Animator.StringToHash("LastMoveY");

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;

    [Header("Velocidades")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float crouchSpeed = 3f;
    public float rollForce = 15f;
    public float JumpForce = 10f;

    [Header("Combate")]
    public int attackDamage = 5;
    public int quickShotDamage = 10;
    public int quickSlideDamage = 5;
    public int specialDamage = 20;
    public int screamDamage = 10;

    [Header("Vida")]
    public int maxHealth = 5;
    public int currentHealth;

    [Header("Tempos de Estado (segundos)")]
    public float attackDuration = 0.4f;
    public float quickShotDuration = 0.5f;
    public float quickSlideDuration = 0.5f;
    public float rollingDuration = 0.4f;
    public float JumpAttackDuration = 0.6f;
    public float screamDuration = 1f;
    public float pummelDuration = 0.6f;
    public float blockDuration = 0.3f;
    public float castSpellDuration = 0.8f;
    public float specialDuration = 1.0f;
    public float takeDamageDuration = 0.4f;
    public float unSheathDuration = 0.8f;

    [Header("Referências")]
    public Transform attackPoint;
    public float attackRange = 1.2f;
    public LayerMask enemyLayers; // Há partes do Foreground que o jogador passa e se esconde.

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;

    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.right;

    private bool isRunning;
    private bool isCrouching;

    private float stateTimer;
    private bool isStateLocked;
    private bool isInvulnerable;
    private bool specialsLoaded;
    private Color originalColor;
    private Coroutine glowRoutine;

    // Timer interno de knockback: bloqueia o movimento e input (redundante kkkk) enquanto durar a animação de dano (Não está aqui)
    // Essa parte confere o HP do jogador se ele toma dano, e então se não tiver mais HP disponível ele vai...
    // ... conferir o PlayerState.Die abaixo.
    private float knockbackTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color; // quando o jogador recebe dano, 
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (CurrentState == PlayerState.Die) return;

        // Knockback quando o jogador recebe dano. Isso aqui vai evitar danos extras se o jogador ficar parado em cima do alvo...
        // Por sinal, isso trava o teclado, "punindo" o jogador.
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            return;
        }

        ReadInput();
        UpdateDirection();

        if (isStateLocked)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                isStateLocked = false;
                isInvulnerable = false;
                ClearGlow();
                DecideDefaultState();
            }
            return;
        }

        EvaluateTransitions();
    }

    private void FixedUpdate()
    {
        // Knockback ativo: não sobrescreve a velocity do empurrão
        if (knockbackTimer > 0f) return;

        ApplyMovement();
    }

    private void ReadInput() // Leitura de outras animações para a última posição do Idle e serem salvas.
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        isRunning = Input.GetKey(KeyCode.LeftShift) && moveInput.magnitude > 0.1f;
        isCrouching = Input.GetKey(KeyCode.C);
    }

    private void UpdateDirection() // Para outras animações usarem a última posição do Idle e serem salvas.
    {
        if (knockbackTimer > 0f) return;

        Vector2 direction = Vector2.zero;
        bool isMoving = moveInput.magnitude > 0.1f;

        if (isMoving)
        {
            direction = moveInput.normalized;
            lastMoveDirection = direction;
        }

        anim.SetFloat("X", lastMoveDirection.x);
        anim.SetFloat("Y", lastMoveDirection.y);
        anim.SetFloat("LastMoveX", lastMoveDirection.x);
        anim.SetFloat("LastMoveY", lastMoveDirection.y);

        if (isStateLocked) return;

        anim.SetBool("isWalking", isMoving && !isRunning && !isCrouching);
        anim.SetBool("isRunning", isMoving && isRunning && !isCrouching);
        anim.SetBool("isCrouching", isCrouching && !isMoving);
        anim.SetBool("isCrouchWalking", isCrouching && isMoving);
    }

    // Os atalhos do teclado para todas as funções de comportamento complexo.
    // É necessário deixar exclusivo essa parte, justamente para evitar equívocos.
    private void EvaluateTransitions()
    {
        if (Input.GetKeyDown(KeyCode.Space)) { EnterJumpAttack(); return; }
        if (Input.GetKeyDown(KeyCode.F)) { EnterAttack(); return; }
        if (Input.GetKeyDown(KeyCode.R)) { EnterQuickShot(); return; }
        if (Input.GetKeyDown(KeyCode.V)) { EnterQuickSlide(); return; }
        if (Input.GetKeyDown(KeyCode.Q)) { EnterScream(); return; }
        if (Input.GetKeyDown(KeyCode.G)) { EnterPummel(); return; }

        if (Input.GetKeyDown(KeyCode.LeftControl) && moveInput.magnitude > 0.1f)
        {
            EnterRolling(); return;
        }

        if (Input.GetKeyDown(KeyCode.Tab)) { EnterUnSheath(); return; }
        if (Input.GetKeyDown(KeyCode.Alpha1) && specialsLoaded) { EnterSpecial1(); return; }
        if (Input.GetKeyDown(KeyCode.Alpha2) && specialsLoaded) { EnterSpecial2(); return; }

        if (isCrouching)
        {
            if (moveInput.magnitude > 0.1f) EnterCrouchRun();
            else EnterCrouchIdle();
            return;
        }

        if (moveInput.magnitude > 0.1f)
        {
            if (isRunning) EnterRun();
            else EnterWalk();
        }
        else
        {
            EnterIdle();
        }
    }

    // Movimentações básicas do jogador.
    private void ApplyMovement()
    {
        if (isStateLocked) return;

        float speed = 0f;
        switch (CurrentState)
        {
            case PlayerState.Walk: speed = walkSpeed; break;
            case PlayerState.Run: speed = runSpeed; break;
            case PlayerState.CrouchRun: speed = crouchSpeed; break;
            case PlayerState.RunBackwards: speed = walkSpeed; break; // Não sei qual atalho iria ser cabível para isso aqui.
            default: speed = 0f; break; // Por motivos óbvios, a velocidade do Idle será 0 pq o jogador está parado.
        }

        rb.linearVelocity = moveInput.normalized * speed;
    }

    private void EnterIdle()
    {
        SetState(PlayerState.Idle);
        rb.linearVelocity = Vector2.zero;
        anim.SetFloat("Speed", 0f);
    }

    // Só em ler o método (EnterWalk) sabemos o que é. Sem mais explicações.
    private void EnterWalk()
    {
        SetState(PlayerState.Walk);
        anim.SetFloat("Speed", 1f);
    }

    // Use Shift para correr. Pode-se iniciar do Idle ou do Walk.
    private void EnterRun()
    {
        SetState(PlayerState.Run);
        anim.SetFloat("Speed", 2f);
    }

    // Agachar com a tecla C. Pode-se movimentar usando as teclas de direção.
    private void EnterCrouchIdle()
    {
        SetState(PlayerState.CrouchIdle);
        rb.linearVelocity = Vector2.zero;
        anim.SetFloat("Speed", 0f);
    }

    // Estado agachado com movimento. Decidi reduzir a velocidade pois não tem sentido correr agachado.
    private void EnterCrouchRun()
    {
        SetState(PlayerState.CrouchRun);
        anim.SetFloat("Speed", 0.5f);
    }

    // Ataque base (Melee) do personagem. Tecla F é um ótimo atalho, não?
    private void EnterAttack()
    {
        SetState(PlayerState.Attack);
        LockState(attackDuration);
        anim.SetTrigger("Attack");
        DealDamageInFront(attackDamage);
    }

    // Ainda não inserido. Como diabos essa seria inserida? kkkkkk
    private void EnterQuickShot()
    {
        SetState(PlayerState.QuickShot);
        LockState(quickShotDuration);
        anim.SetTrigger("QuickShot");
        DealDamageInFront(quickShotDamage);
    }

    // QuickSlide precisa estar com o Crouch e CrouchWalk antecipados para executar.
    // Funcionando corretamente, mas o sprite está maior queo normal. Não sei como diminuir o tamanho do sprite... Mantenha como está.
    private void EnterQuickSlide()
    {
        SetState(PlayerState.QuickSlide);
        LockState(quickSlideDuration);
        anim.SetTrigger("QuickSlide");
        rb.linearVelocity = moveInput.normalized * rollForce;
        DealDamageInFront(quickSlideDamage);
    }

    // Animação feita para recarregar os speciais.
    // Scream está substituindo o UnSheath.
    private void EnterScream()
    {
        SetState(PlayerState.Scream);
        LockState(screamDuration);
        anim.SetTrigger("Scream");
        DealDamageInFront(screamDamage);
    }

    // Ainda não inserido no Unity. Confira a sprite sheet para ver o que vai dar pra fazer...
    private void EnterPummel()
    {
        SetState(PlayerState.Pummel);
        LockState(pummelDuration);
        anim.SetTrigger("Pummel");
        DealDamageInFront(attackDamage);
    }

    // Inserido no Unity e muito bem executado!
    private void EnterJumpAttack()
    {
        SetState(PlayerState.JumpAttack);
        LockState(JumpAttackDuration);
        anim.SetTrigger("JumpKick");
        rb.linearVelocity = moveInput.normalized * JumpForce;
    }

    // Ainda não inserido no Unity.
    private void EnterRolling()
    {
        SetState(PlayerState.Rolling);
        LockState(rollingDuration);
        isInvulnerable = true;
        StartGlow(Color.cyan);
        anim.SetTrigger("Rolling");
        rb.linearVelocity = moveInput.normalized * rollForce;
    }

    // Animação de recarregar os ataques especiais.
    // Animação não inserida, pois faz mais sentido o uso da animação Scream faz melhor pra um Melee.
    private void EnterUnSheath()
    {
        SetState(PlayerState.UnSheath);
        LockState(unSheathDuration);
        anim.SetTrigger("UnSheath");
        StartCoroutine(LoadSpecialsAfterDelay());
    }

    // Após a animação de qualquer estado, o Enum precisa definir os tempos (EM 0f) para todos eles.
    private IEnumerator LoadSpecialsAfterDelay()
    {
        yield return new WaitForSeconds(unSheathDuration);
        specialsLoaded = true;
    }

    private void EnterSpecial1()
    {
        SetState(PlayerState.Special1);
        LockState(specialDuration);
        specialsLoaded = false;
        StartGlow(Color.green);
        anim.SetTrigger("Special1");
        DealDamageInFront(specialDamage);
    }

    private void EnterSpecial2()
    {
        SetState(PlayerState.Special2);
        LockState(specialDuration);
        specialsLoaded = false;
        StartGlow(Color.white);
        anim.SetTrigger("Special2");
        DealDamageInFront(specialDamage);
    }

    public void TakeDamage(int damage) 
    {
        if (isInvulnerable || CurrentState == PlayerState.Die) return;
        // Inseri isso aqui para o jogador não receber mais dano que o normal.
        // Se você entra em contato com a chama, você piscará vermelho e se afastará do alvo que lhe deu dano.

        currentHealth -= damage;
        StartCoroutine(GlowRoutine(Color.red, takeDamageDuration));

        if (currentHealth <= 0)
        {
            EnterDie();
            return;
        }

        SetState(PlayerState.TakeDamage);
        LockState(takeDamageDuration);
        anim.SetTrigger("TakeDamage");
    }

    // âncora matriz da animação de morte.
    private void EnterDie()
    {
        SetState(PlayerState.Die);
        isStateLocked = true;
        rb.linearVelocity = Vector2.zero;
        anim.SetTrigger("Die");
    }

    private void DealDamageInFront(int damage)
    {
        if (attackPoint == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position, attackRange, enemyLayers);

        foreach (var hit in hits) // Bloqueia o jogador de receber mais dano que o esperado, NÃO REMOVE ISSO EM NOME DE JESUS!!!
        {
            hit.GetComponent<EnemyHealth>()?.TakeDamage(damage);
        }
    }

    private void SetState(PlayerState newState) // Aguarda o jogador acessar outros estados de máquina, evitando conflitos.
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
    }

    private void LockState(float duration) // Privado, pois somente o Player vai ter acesso a isso.
    {
        isStateLocked = true;
        stateTimer = duration; // Fazer os testes de animação para verificar se está fluindo bem. Definir novos valores caso esteja ruim.
    }

    private void DecideDefaultState()
    {
        if (moveInput.magnitude > 0.1f)
        {
            if (isCrouching) EnterCrouchRun();
            else if (isRunning) EnterRun();
            else EnterWalk();
        }
        else
        {
            if (isCrouching) EnterCrouchIdle();
            else EnterIdle();
        }
    }

    // Âncora para o jogador brilhar enquanto recebe dano. Tive que por isso aqui pra realmente funcionar.
    private void StartGlow(Color color)
    {
        if (glowRoutine != null) StopCoroutine(glowRoutine);
        glowRoutine = StartCoroutine(GlowRoutine(color, 999f));
    }

    private IEnumerator GlowRoutine(Color color, float duration)
    {
        sr.color = color;
        yield return new WaitForSeconds(duration);
        sr.color = originalColor;
    }

    private void ClearGlow() // Depois de receber dano, o jogador volta à sua cor original (QUANDO SAIR DA ANIMAÇÃO DE TAKEDAMAGE)
    {
        if (glowRoutine != null) StopCoroutine(glowRoutine);
        sr.color = originalColor;
    }

    public bool IsInvulnerable => isInvulnerable;

    // Solução para ancorar a classe PlayerHealth.cs a esse código aqui, tornando aqui a matriz.

    public Vector2 LastMoveDirection => lastMoveDirection;

    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        if (direction.sqrMagnitude < 0.01f) return;

        rb.linearVelocity = direction.normalized * force;
        knockbackTimer = duration;
        isStateLocked = true;
        stateTimer = duration;
    }

    public void FlashRed(float duration) // Aqui, dá-se o red glow quando o jogador recebe danod do inimigo.
    {
        if (glowRoutine != null) StopCoroutine(glowRoutine);
        glowRoutine = StartCoroutine(GlowRoutine(Color.red, duration));
    }
    public void PlayTakeDamageAnimation()
    // Trigger do TakeDamage. NÃO MUDE ESSA POHA!!!
    // O de cima não estava fazendo a animação de TakeDamage aparecer, então tive que duplicar pra funcionar...
    {
        SetState(PlayerState.TakeDamage);
        LockState(takeDamageDuration);
        anim.SetTrigger("TakeDamage");
    }
    public void Die() // Depois de inserir a animação de receber dano, também é necessário trazer a animação de morte.
    {
        if (CurrentState == PlayerState.Die) return;
        EnterDie();
        anim.SetBool("isDead", true);
    }
}