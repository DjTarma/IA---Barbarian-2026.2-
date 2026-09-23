using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] // Como exploramos o battlefield, é mais que importante inserir isso como requisito.
[RequireComponent(typeof(Animator))] // Temos animações e triggers e bools para elas. Outro comando importante a ser inserido.
[RequireComponent(typeof(SpriteRenderer))] // Renderizador de sprite 2D para o projeto.

public class PlayerController : MonoBehaviour // Classe principal do arquivo. Não mexe nisso em nome de Jesus!
{
    public PlayerState CurrentState { get; private set; } = PlayerState.Idle; // Estado inicial da gameplay e do Animator.

    [Header("Velocidades")] // String de valores de velocidades que os estados de movimentação possuem.
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float crouchSpeed = 2f;
    public float rollForce = 7f;
    public float JumpForce = 7f;
    public float kickSpeed = 3f;

    [Header("Combate")]                 // String de valores de dano.
                                        // Aplicação de dano exercido pelo Player a entidades que forem inseridas no futuro.
    public int attackDamage = 5;        // Ataque com a tecla F, meio que é o "Main Attack".
    public int quickShotDamage = 7;     // O jogador futuramente vai lançar o machado na direção que olhar.
                                        // Não achei nenhum asset que combina, então, deixamos pro futuro.
    public int quickSlideDamage = 3;    // Dano para quebrar vasos e caixas de madeiras que forem inseridas.
    public int specialDamage = 15;      // Special1 e Special2 usam a mesma âncora
    public int screamDamage = 0;        // Está em zero, pois podemos trazer criaturas que fujam do jogador futuramente.
    public int kickDamage = 3;

    [Header("Tempos de Estado (segundos)")] // String que trazemos no insepctor o tempo de estado em segundos.
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
    public float kickDuration = 0.4f;

    [Header("Efeitos Especiais")]   // Ainda não conseguir fazer essa string ficar funcional 100%
    public GameObject special1EffectPrefab;
    public GameObject special2EffectPrefab;
    public float specialEffectDuration = 1f;

    [Header("Screen Shake")]        // String para sacudir a tela enquanto o Special2 for executado. Combinaria com o JumpAttack? :thinking:
    public float special2ShakeDuration = 0.3f;
    public float special2ShakeMagnitude = 0.2f;

    [Header("Referências")]
    public Transform attackPoint;       // Declaração de variável usada para todas os bools (true/false) que são para dar dano.
    public float attackRange = 1.2f;    // 1 tile e 1/5 de range de dano para a direção que olhamos para qualquer alvo.
    public LayerMask enemyLayers;       // Inimigos precisariam estar nessa camada (Que o jogador estiver ) para receber dano.

    // Variáveis privadas para referenciar componentes da Unity. Ao botar esse script no Player, automaticamente são implementados.
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;

    private Vector2 moveInput;      // X e Y. moveInput é a variável de entrada do teclado. :smile:
    private Vector2 lastMoveDirection = Vector2.right;  // O jogo começa com o jogador olhando pra direita.

    private bool isRunning;         // true/false para estado de corrida.
    private bool isCrouching;       // true/false para estado de agachar.

    private float stateTimer;       // Variável para guardar em decimal a duração dos estados de máquina.
    private bool isStateLocked;     // Existem estados no jogo que o jogador não pode executar outros.
    private bool isInvulnerable;    // Há estados no jogo que protege o jogador de receber dano ou um dano maior que ele deveria receber.
    private bool specialsLoaded;    // Usado na variável Scream, o jogador precisa usar a tecla Q para executar.
    private Color originalColor;    // Usado para fazer a cor padrão do jogador ser a que está sendo executada.
    private Coroutine glowRoutine;  // Precisa da variável de cima, com alguns estados ter transformações de cor enquanto a animação executa.
    private float knockbackTimer;   // Tempo de knockback, estado que faz o jogador ser lançado para a direção oposta quando recebe dano ou segura dano. O jogador não se move enquanto isso executa.

    private void Awake()            // Método que é usado quando o objeto inicializa. Todos os componentes abaixo são os que o jogo usa em geral.
                                    // É o método que salva a minha vida, kkkk
    {
        rb = GetComponent<Rigidbody2D>();       // componente que define limites de interação físicas.
        anim = GetComponent<Animator>();        // responsável no GameObject para executar animações.
        sr = GetComponent<SpriteRenderer>();    // responsável em renderizar os objetos gráficos em sprites. poderia ter flip.X (para espelhar em horizontal a imagem)
        originalColor = sr.color;
    }

    private void Update()
    {
        if (CurrentState == PlayerState.Die) return;    // Estado de morte. Confira e defina a quantidade de vida na classe PlayerHealth.cs

        if (knockbackTimer > 0f)                        // Responsável em fazer o jogador não se mover)
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
        if (knockbackTimer > 0f) return;
        ApplyMovement();
    }

    private void ReadInput()                            // Leitor de entradas de movimentação em geral.
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");   
        moveInput.y = Input.GetAxisRaw("Vertical");

        isRunning = Input.GetKey(KeyCode.LeftShift) && moveInput.magnitude > 0.1f;
        isCrouching = Input.GetKey(KeyCode.C);
    }

    private void UpdateDirection()
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

    private void EvaluateTransitions()
    {
        // JumpAttack executa ao se movimentar MENOS se estiver agachado/agachandoAndando.
        if (Input.GetKeyDown(KeyCode.Space) && !isCrouching) { EnterJumpAttack(); return; }

        if (Input.GetKeyDown(KeyCode.F)) { EnterAttack(); return; }
        if (Input.GetKeyDown(KeyCode.Y)) { EnterQuickShot(); return; }

        // Kick só executa em pé (sendo idle ou walk/run), nunca agachado. Não tenho problema em pessoas agachadas, mas o ideal é ter lógica nas coisas.
        if (Input.GetKeyDown(KeyCode.X) && !isCrouching) { EnterKick(); return; }

        if (Input.GetKeyDown(KeyCode.V) && isCrouching && moveInput.magnitude > 0.1f) // A prova que eu não tenho nada contra com quem anda agachado. Você desliza ao andar agachado.
        {
            EnterQuickSlide(); return;
        }

        if (Input.GetKeyDown(KeyCode.Q) && moveInput.magnitude < 0.1f && !isCrouching && !isRunning) // Só executa andando ou idle.
        {
            EnterScream(); return;
        }

        if (Input.GetKeyDown(KeyCode.G)) { EnterPummel(); return; } // Não inserido. Vou deixar pro futuro.

        if (Input.GetKeyDown(KeyCode.E) && moveInput.magnitude > 0.1f) // Só executa andando. Esse aqui é o Rolling.
        {
            EnterRolling(); return;
        }

        if (Input.GetKeyDown(KeyCode.Tab)) { EnterUnSheath(); return; } // Não inserido.
        if (Input.GetKeyDown(KeyCode.R) && specialsLoaded) { EnterSpecial1(); return; } // Tecla R
        if (Input.GetKeyDown(KeyCode.T) && specialsLoaded) { EnterSpecial2(); return; } // Tecla T

        if (isCrouching) // Tecla C.
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

    private void ApplyMovement()
    {
        // Decidi fazer uma exceção; durante o Kick o player pode se mover (devagarinho),
        // então NÃO bloqueia o movimento mesmo com isStateLocked == true.
        if (isStateLocked && CurrentState != PlayerState.Kick) return;

        float speed = 0f;
        switch (CurrentState)
        {
            case PlayerState.Walk: speed = walkSpeed; break;
            case PlayerState.Run: speed = runSpeed; break;
            case PlayerState.CrouchRun: speed = crouchSpeed; break;
            case PlayerState.RunBackwards: speed = walkSpeed; break;
            case PlayerState.Kick: speed = kickSpeed; break;
            default: speed = 0f; break;
        }

        rb.linearVelocity = moveInput.normalized * speed;
    }

    private void EnterIdle()        // Estado Idle.
    {
        SetState(PlayerState.Idle);
        rb.linearVelocity = Vector2.zero;
        anim.SetFloat("Speed", 0f);
    }

    private void EnterWalk()        // Estado de caminhar (Walk).
    {
        SetState(PlayerState.Walk);
        anim.SetFloat("Speed", 1f);
    }

    private void EnterRun()         // Estado de corrida (Run)
    {
        SetState(PlayerState.Run);
        anim.SetFloat("Speed", 2f);
    }

    private void EnterCrouchIdle()  // Estado de Agachar (parado)
    {
        SetState(PlayerState.CrouchIdle);
        rb.linearVelocity = Vector2.zero;
        anim.SetFloat("Speed", 0f);
    }

    private void EnterCrouchRun()   // Estado de Agachar andando.
    {
        SetState(PlayerState.CrouchRun);
        anim.SetFloat("Speed", 0.5f);
    }

    private void EnterAttack()      // Estado de atacar.
    {
        SetState(PlayerState.Attack);
        LockState(attackDuration);
        anim.SetTrigger("Attack");
        DealDamageInFront(attackDamage);    // Só ataca na direção do jogador. Está definido no topo que é 1,2 tiles.
    }

    private void EnterQuickShot()           // Não inserido. Não achei nenhum asset de machado para completar isso aqui.
    {
        SetState(PlayerState.QuickShot);
        LockState(quickShotDuration);
        anim.SetTrigger("QuickShot");
        DealDamageInFront(quickShotDamage);
    }

    private void EnterQuickSlide()          // Deslizar enquanto está agachado se movendo.
    {
        SetState(PlayerState.QuickSlide);
        LockState(quickSlideDuration);
        anim.SetTrigger("QuickSlide");
        rb.linearVelocity = moveInput.normalized * rollForce;
        DealDamageInFront(quickSlideDamage);
    }

    private void EnterKick()                // Chutar. Nada mais a declarar aqui, kkkk  
    {
        SetState(PlayerState.Kick);
        LockState(kickDuration);
        anim.SetTrigger("Kick");
        DealDamageInFront(kickDamage);
    }

    private void EnterScream()              // Scream (gritar) Observe as outras variáveis abaixo.
    {
        SetState(PlayerState.Scream);
        LockState(screamDuration);
        StartGlow(Color.cyan, specialDuration);
        anim.SetTrigger("Scream");
        StartCoroutine(LoadSpecialsAfterDelay());
    }

    private void EnterPummel()              // Não inserido.
    {
        SetState(PlayerState.Pummel);
        LockState(pummelDuration);
        anim.SetTrigger("Pummel");
        DealDamageInFront(attackDamage);
    }

    private void EnterJumpAttack()          // JumpAttack, um dos melhores front moves que estão no projeto.
    {
        SetState(PlayerState.JumpAttack);
        LockState(JumpAttackDuration);
        anim.SetTrigger("JumpKick");
        rb.linearVelocity = moveInput.normalized * JumpForce;
    }

    private void EnterRolling()             // Rolling, estado que funciona andando e correndo.
    {
        SetState(PlayerState.Rolling);
        LockState(rollingDuration);
        isInvulnerable = true;
        anim.SetTrigger("Rolling");
        rb.linearVelocity = moveInput.normalized * rollForce;
    }

    private void EnterUnSheath()            // Não inserido.
    {
        SetState(PlayerState.UnSheath);
        LockState(unSheathDuration);
        anim.SetTrigger("UnSheath");
        StartCoroutine(LoadSpecialsAfterDelay());
    }

    private IEnumerator LoadSpecialsAfterDelay()    // Definir limites de tempo para os Special1 e Special2 forem inseridos. Scream tem isso também.
    {
        yield return new WaitForSeconds(unSheathDuration);
        specialsLoaded = true;
    }

    private void EnterSpecial1()        // Special1 com a tecla R. Só ativa com o Scream ativado.
    {
        SetState(PlayerState.Special1);
        LockState(specialDuration);
        specialsLoaded = false;
        StartGlow(Color.purple, specialDuration);
        anim.SetTrigger("Special1");
        DealDamageInFront(specialDamage);
        SpawnEffect(special1EffectPrefab);
    }

    private void EnterSpecial2()        // Special2 com a tecla T. Só ativa com o Scream ativado.
    {
        SetState(PlayerState.Special2);
        LockState(specialDuration);
        specialsLoaded = false;
        StartGlow(Color.blue, specialDuration);
        anim.SetTrigger("Special2");
        DealDamageInFront(specialDamage);
        SpawnEffect(special2EffectPrefab);

        if (CameraShake.Instance != null) // Consertar isso aqui.
            CameraShake.Instance.Shake(special2ShakeDuration, special2ShakeMagnitude);
    }

    private void EnterDie()             // Player morre.
    {
        SetState(PlayerState.Die);
        isStateLocked = true;                   // Estado trava qualquer outro que tentar trazer. Não deixe nenhuma passada pra outro estado no Animator.
        rb.linearVelocity = Vector2.zero;       // Faz o jogador não sair do lugar, já que o Vector2 está com valor zero.
        anim.SetTrigger("Die");
    }

    private void DealDamageInFront(int damage)  // Dar dano.
    {
        if (attackPoint == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(     // "range" para detectar objetos que recebem/dão dano.
            attackPoint.position, attackRange, enemyLayers); // Checa os colliders no alcance.

        foreach (var hit in hits)       // Loop que confere a variável hit em hits (Um overlap para todos os objetos com Collider?).
        {
            hit.GetComponent<EnemyHealth>()?.TakeDamage(damage); // Vai conferir qualquer entidade que tenha o script EnemyHealth.
        }
    }

    private void SetState(PlayerState newState) // Após o estado executar algo, chama um enum como idle, walk, etc...
    {
        if (CurrentState == newState) return;
        CurrentState = newState;        // Aqui tá a mágica. Ele meio que joga pro Animator o que precisa fazer.
    }

    private void LockState(float duration)  // Tempo de execução das animações.
    {
        isStateLocked = true;
        stateTimer = duration;
    }

    private void DecideDefaultState() // O jogador terminou um estado (Attack, Kick, TakeDamage, etc.). Pra onde ele vai agora?
    {
        if (moveInput.magnitude > 0.1f) // velocidade acima de 0.1f
        {
            if (isCrouching) EnterCrouchRun();
            else if (isRunning) EnterRun();
            else EnterWalk();
        }
        else    // Ou
        {
            if (isCrouching) EnterCrouchIdle();
            else EnterIdle();
        }
    }

    private void StartGlow(Color color, float duration) // aqui é onde a magia acontece². Esse método sem retorno é quais são as animações que vão ter cores exclusivas.
    {
        if (glowRoutine != null) StopCoroutine(glowRoutine);
        glowRoutine = StartCoroutine(GlowRoutine(color, duration));
    }

    private IEnumerator GlowRoutine(Color color, float duration)    // Enum que define a duração do tempo dos estados que vão ter cores.
    {
        sr.color = color;
        yield return new WaitForSeconds(duration);
        sr.color = originalColor;
    }

    private void ClearGlow()        // Método que reseta para a cor original do sprite.
    {
        if (glowRoutine != null) StopCoroutine(glowRoutine);
        sr.color = originalColor;
    }

    private void SpawnEffect(GameObject prefab)     // Quando o jogador entra numa fase OU morre, ao resetar a partida, você reinicia na posição original.
    {
        if (prefab == null) return;

        GameObject fx = Instantiate(prefab, transform.position, Quaternion.identity);
        Destroy(fx, specialEffectDuration);
    }

    // Âncora usada com o PlayerHeath. Aqui trago os condicionais dos estados.

    public bool IsInvulnerable => isInvulnerable; // Definição de invulnerabilidade ao tomar dano.
    public Vector2 LastMoveDirection => lastMoveDirection; // A animação executa conforme a posição estabelecida.

    public void ApplyKnockback(Vector2 direction, float force, float duration) // Knockback executa jogando o player para trás.
    {
        if (direction.sqrMagnitude < 0.01f) return; // Um tile para trás. Não há necessidade mais do que isso...

        rb.linearVelocity = direction.normalized * force;
        knockbackTimer = duration;
        isStateLocked = true;
        stateTimer = duration;
    }

    public void FlashRed(float duration) // Ao receber dano, o jogador deverá brilhar vermelho.
    {
        if (glowRoutine != null) StopCoroutine(glowRoutine); // GlowRoutine é o comando que executa a cor de dano no jogador ou até em um inimigo no futuro.
        glowRoutine = StartCoroutine(GlowRoutine(Color.red, duration)); // Aqui eu defino qual cor é usada. Não tive interesse em por valores Hex.
    }

    public void PlayTakeDamageAnimation() // Execução da animação de receber dano.
    {
        if (CurrentState == PlayerState.TakeDamage) return;
        SetState(PlayerState.TakeDamage);
        LockState(takeDamageDuration); // Isso segura tanto o estado de invulnerabilidade quanto o da animação.
        anim.SetTrigger("TakeDamage"); // Disparador da animação
    }

    public void Die() // Se o jogador perde os cinco pontos de vida definidos no PlayerHealth, a animação de morte executa.
    {
        if (CurrentState == PlayerState.Die) return;
        EnterDie(); // Jogador morre...
        anim.SetBool("isDead", true); // Para o animator! Colocar isso aqui em "AnyState" do animator, pfvr.
    }
}