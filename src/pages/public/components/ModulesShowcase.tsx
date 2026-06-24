import { useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { DraftingCompass, Cpu, Globe, Coins, ShoppingCart, Award, ArrowRight } from 'lucide-react';
import { Section, SectionHeader } from './Section';

type ModuleType = 'cad' | 'mrp' | 'b2b' | 'finance' | 'procure' | 'service';

type Stat = { label: string; value: string };

type ModuleDef = {
  id: ModuleType;
  icon: ReactNode;
  title: string;
  desc: string;
  long: string;
  color: string;
  accentBar: string;
  stats: Stat[];
  ui: ReactNode;
};

const StatTiles = ({ stats }: { stats: Stat[] }) => (
  <dl className="ca-stagger grid grid-cols-3 gap-2">
    {stats.map((s, i) => (
      <div
        key={i}
        className="rounded-xl border border-slate-200/70 bg-white/60 px-3 py-2.5 text-center dark:border-slate-800/70 dark:bg-slate-900/50"
      >
        <dt className="text-[15px] font-extrabold tracking-tight text-slate-900 dark:text-white">
          {s.value}
        </dt>
        <dd className="mt-0.5 text-[10px] font-medium leading-tight text-slate-500 dark:text-slate-400">
          {s.label}
        </dd>
      </div>
    ))}
  </dl>
);

const FlowDot = ({ d, dur, delay = '0s' }: { d: string; dur: string; delay?: string }) => (
  <circle r="2.4" className="fill-primary-500">
    <animateMotion dur={dur} begin={delay} repeatCount="indefinite" path={d} />
  </circle>
);

const CadSchematic = ({ caption }: { caption: string }) => (
  <figure className="m-0">
    <svg
      viewBox="0 0 320 150"
      className="h-auto w-full"
      role="img"
      aria-hidden="true"
      preserveAspectRatio="xMidYMid meet"
    >
      <defs>
        <pattern id="caGrid" width="20" height="20" patternUnits="userSpaceOnUse">
          <path
            d="M20 0H0V20"
            className="stroke-slate-300/50 dark:stroke-slate-700/60"
            fill="none"
            strokeWidth="1"
          />
        </pattern>
      </defs>
      <rect x="0" y="0" width="320" height="150" fill="url(#caGrid)" />
      <g className="fill-primary-500/10 stroke-primary-500" strokeWidth="2">
        <rect x="60" y="28" width="90" height="94" rx="3" className="animate-fade-in" />
        <rect x="170" y="28" width="90" height="94" rx="3" className="animate-fade-in" />
      </g>
      <line
        x1="155"
        y1="22"
        x2="155"
        y2="128"
        className="stroke-accent-500"
        strokeWidth="1.5"
        strokeDasharray="4 4"
      />
      <g className="fill-primary-600 dark:fill-primary-400">
        <circle cx="60" cy="28" r="3.5" className="animate-pulse-soft" />
        <circle cx="150" cy="28" r="3.5" className="animate-pulse-soft" />
        <circle cx="60" cy="122" r="3.5" className="animate-pulse-soft" />
        <circle cx="150" cy="122" r="3.5" className="animate-pulse-soft" />
      </g>
      <path
        d="M150 122 A18 18 0 0 0 168 104"
        className="stroke-accent-500"
        fill="none"
        strokeWidth="1.5"
      />
      <text x="160" y="100" className="fill-accent-600 dark:fill-accent-400" fontSize="9">
        90°
      </text>
      <text x="105" y="20" className="fill-slate-500 dark:fill-slate-400" fontSize="8">
        3200 mm
      </text>
    </svg>
    <figcaption className="mt-3 text-center text-[11px] text-slate-500 dark:text-slate-400">
      {caption}
    </figcaption>
  </figure>
);

const MrpSchematic = ({ caption }: { caption: string }) => (
  <figure className="m-0">
    <svg
      viewBox="0 0 320 150"
      className="h-auto w-full"
      role="img"
      aria-hidden="true"
      preserveAspectRatio="xMidYMid meet"
    >
      <rect
        x="10"
        y="14"
        width="180"
        height="122"
        rx="6"
        className="fill-slate-50 stroke-slate-300 dark:fill-slate-900/60 dark:stroke-slate-700"
        strokeWidth="1.5"
      />
      <g className="fill-primary-500/20 stroke-primary-500" strokeWidth="1.2">
        <rect x="18" y="22" width="70" height="48" rx="2" className="animate-zoom-in" />
        <rect x="92" y="22" width="44" height="48" rx="2" className="animate-zoom-in" />
        <rect x="140" y="22" width="42" height="70" rx="2" className="animate-zoom-in" />
        <rect x="18" y="74" width="50" height="56" rx="2" className="animate-zoom-in" />
        <rect x="72" y="74" width="64" height="56" rx="2" className="animate-zoom-in" />
        <rect x="140" y="96" width="42" height="34" rx="2" className="animate-zoom-in" />
      </g>
      <g className="fill-success-500/15 stroke-success-500" strokeWidth="1" strokeDasharray="3 3">
        <rect x="138" y="120" width="44" height="10" rx="1" />
      </g>
      <rect
        x="208"
        y="40"
        width="104"
        height="70"
        rx="8"
        className="fill-warning-500/10 stroke-warning-500"
        strokeWidth="1.5"
      />
      <text
        x="260"
        y="34"
        textAnchor="middle"
        className="fill-slate-500 dark:fill-slate-400"
        fontSize="8"
      >
        Temper Fırını
      </text>
      <g className="stroke-warning-500" strokeWidth="3" strokeLinecap="round">
        <line x1="222" y1="98" x2="222" y2="78">
          <animate attributeName="y2" values="98;72;98" dur="1.8s" repeatCount="indefinite" />
        </line>
        <line x1="240" y1="98" x2="240" y2="66">
          <animate attributeName="y2" values="98;62;98" dur="2.1s" repeatCount="indefinite" />
        </line>
        <line x1="258" y1="98" x2="258" y2="82">
          <animate attributeName="y2" values="98;76;98" dur="1.5s" repeatCount="indefinite" />
        </line>
        <line x1="276" y1="98" x2="276" y2="70">
          <animate attributeName="y2" values="98;64;98" dur="2.3s" repeatCount="indefinite" />
        </line>
        <line x1="294" y1="98" x2="294" y2="86">
          <animate attributeName="y2" values="98;80;98" dur="1.7s" repeatCount="indefinite" />
        </line>
      </g>
    </svg>
    <figcaption className="mt-3 text-center text-[11px] text-slate-500 dark:text-slate-400">
      {caption}
    </figcaption>
  </figure>
);

const B2bSchematic = ({ caption }: { caption: string }) => {
  const spokes = [
    { x: 286, y: 30, d: 'M160 75 L286 30' },
    { x: 300, y: 75, d: 'M160 75 L300 75' },
    { x: 286, y: 120, d: 'M160 75 L286 120' },
    { x: 40, y: 30, d: 'M160 75 L40 30' },
    { x: 26, y: 75, d: 'M160 75 L26 75' },
    { x: 40, y: 120, d: 'M160 75 L40 120' },
  ];
  return (
    <figure className="m-0">
      <svg
        viewBox="0 0 320 150"
        className="h-auto w-full"
        role="img"
        aria-hidden="true"
        preserveAspectRatio="xMidYMid meet"
      >
        {spokes.map((s, i) => (
          <line
            key={`l${i}`}
            x1="160"
            y1="75"
            x2={s.x}
            y2={s.y}
            className="stroke-slate-300 dark:stroke-slate-700"
            strokeWidth="1.2"
          />
        ))}
        {spokes.map((s, i) => (
          <FlowDot key={`d${i}`} d={s.d} dur={`${2 + (i % 3) * 0.4}s`} delay={`${i * 0.2}s`} />
        ))}
        {spokes.map((s, i) => (
          <g key={`n${i}`}>
            <circle
              cx={s.x}
              cy={s.y}
              r="9"
              className="fill-success-500/15 stroke-success-500"
              strokeWidth="1.4"
            />
            <circle cx={s.x} cy={s.y} r="3" className="fill-success-600 dark:fill-success-400" />
          </g>
        ))}
        <circle
          cx="160"
          cy="75"
          r="22"
          className="fill-primary-500/15 stroke-primary-500"
          strokeWidth="2"
        />
        <circle cx="160" cy="75" r="22" className="fill-none stroke-primary-500/40">
          <animate attributeName="r" values="22;30;22" dur="2.4s" repeatCount="indefinite" />
          <animate attributeName="opacity" values="0.5;0;0.5" dur="2.4s" repeatCount="indefinite" />
        </circle>
        <text
          x="160"
          y="79"
          textAnchor="middle"
          className="fill-primary-600 dark:fill-primary-300"
          fontSize="9"
          fontWeight="700"
        >
          HUB
        </text>
      </svg>
      <figcaption className="mt-3 text-center text-[11px] text-slate-500 dark:text-slate-400">
        {caption}
      </figcaption>
    </figure>
  );
};

const FinanceSchematic = ({ caption }: { caption: string }) => (
  <figure className="m-0">
    <svg
      viewBox="0 0 320 150"
      className="h-auto w-full"
      role="img"
      aria-hidden="true"
      preserveAspectRatio="xMidYMid meet"
    >
      <line
        x1="160"
        y1="20"
        x2="160"
        y2="120"
        className="stroke-slate-400 dark:stroke-slate-600"
        strokeWidth="2"
      />
      <polygon points="150,120 170,120 160,134" className="fill-slate-400 dark:fill-slate-600" />
      <g className="origin-center">
        <line
          x1="70"
          y1="34"
          x2="250"
          y2="34"
          className="stroke-slate-400 dark:stroke-slate-600"
          strokeWidth="2"
        >
          <animateTransform
            attributeName="transform"
            type="rotate"
            values="-3 160 34;3 160 34;-3 160 34"
            dur="3.4s"
            repeatCount="indefinite"
          />
        </line>
      </g>
      <g className="fill-success-500/15 stroke-success-500" strokeWidth="1.4">
        <rect x="40" y="40" width="64" height="40" rx="4">
          <animateTransform
            attributeName="transform"
            type="translate"
            values="0 6;0 -6;0 6"
            dur="3.4s"
            repeatCount="indefinite"
          />
        </rect>
      </g>
      <g className="fill-warning-500/15 stroke-warning-500" strokeWidth="1.4">
        <rect x="216" y="40" width="64" height="40" rx="4">
          <animateTransform
            attributeName="transform"
            type="translate"
            values="0 -6;0 6;0 -6"
            dur="3.4s"
            repeatCount="indefinite"
          />
        </rect>
      </g>
      <text
        x="72"
        y="64"
        textAnchor="middle"
        className="fill-success-700 dark:fill-success-300"
        fontSize="9"
        fontWeight="700"
      >
        BORÇ
      </text>
      <text
        x="248"
        y="64"
        textAnchor="middle"
        className="fill-warning-700 dark:fill-warning-300"
        fontSize="9"
        fontWeight="700"
      >
        ALACAK
      </text>
      <text
        x="160"
        y="148"
        textAnchor="middle"
        className="fill-slate-500 dark:fill-slate-400"
        fontSize="9"
      >
        Bilanço Dengesi = 0
      </text>
    </svg>
    <figcaption className="mt-3 text-center text-[11px] text-slate-500 dark:text-slate-400">
      {caption}
    </figcaption>
  </figure>
);

const ProcureSchematic = ({ caption }: { caption: string }) => {
  const bars = [
    { x: 30, h: 70, cls: 'fill-success-500', label: '3.20' },
    { x: 90, h: 84, cls: 'fill-slate-400 dark:fill-slate-600', label: '3.45' },
    { x: 150, h: 96, cls: 'fill-slate-400 dark:fill-slate-600', label: '3.58' },
    { x: 210, h: 110, cls: 'fill-slate-400 dark:fill-slate-600', label: '3.72' },
  ];
  return (
    <figure className="m-0">
      <svg
        viewBox="0 0 320 150"
        className="h-auto w-full"
        role="img"
        aria-hidden="true"
        preserveAspectRatio="xMidYMid meet"
      >
        <line
          x1="20"
          y1="122"
          x2="300"
          y2="122"
          className="stroke-slate-300 dark:stroke-slate-700"
          strokeWidth="1.5"
        />
        {bars.map((b, i) => (
          <g key={i}>
            <rect x={b.x} y={122 - b.h} width="40" height={b.h} rx="3" className={b.cls}>
              <animate
                attributeName="height"
                from="0"
                to={b.h}
                dur="0.7s"
                begin={`${i * 0.12}s`}
                fill="freeze"
              />
              <animate
                attributeName="y"
                from="122"
                to={122 - b.h}
                dur="0.7s"
                begin={`${i * 0.12}s`}
                fill="freeze"
              />
            </rect>
            <text
              x={b.x + 20}
              y="136"
              textAnchor="middle"
              className="fill-slate-500 dark:fill-slate-400"
              fontSize="8"
            >
              €{b.label}
            </text>
          </g>
        ))}
        <g className="animate-fade-in">
          <rect
            x="22"
            y="14"
            width="80"
            height="16"
            rx="8"
            className="fill-success-500/15 stroke-success-500"
            strokeWidth="1"
          />
          <text x="62" y="25" textAnchor="middle" className="fill-success-600" fontSize="8">
            En İyi Teklif
          </text>
        </g>
      </svg>
      <figcaption className="mt-3 text-center text-[11px] text-slate-500 dark:text-slate-400">
        {caption}
      </figcaption>
    </figure>
  );
};

const ServiceSchematic = ({ caption }: { caption: string }) => {
  const steps = [
    { x: 36, label: 'Talep', done: true },
    { x: 118, label: 'Atama', done: true },
    { x: 200, label: 'Onarım', done: true },
    { x: 284, label: 'Kabul', done: false },
  ];
  return (
    <figure className="m-0">
      <svg
        viewBox="0 0 320 150"
        className="h-auto w-full"
        role="img"
        aria-hidden="true"
        preserveAspectRatio="xMidYMid meet"
      >
        <line
          x1="36"
          y1="60"
          x2="284"
          y2="60"
          className="stroke-slate-300 dark:stroke-slate-700"
          strokeWidth="2"
        />
        <line x1="36" y1="60" x2="200" y2="60" className="stroke-success-500" strokeWidth="3">
          <animate attributeName="x2" from="36" to="200" dur="1.4s" fill="freeze" />
        </line>
        {steps.map((s, i) => (
          <g key={i}>
            <circle
              cx={s.x}
              cy="60"
              r="11"
              className={
                s.done
                  ? 'fill-success-500/20 stroke-success-500'
                  : 'fill-primary-500/15 stroke-primary-500'
              }
              strokeWidth="1.6"
            >
              {!s.done && (
                <animate attributeName="r" values="11;13;11" dur="1.6s" repeatCount="indefinite" />
              )}
            </circle>
            <circle
              cx={s.x}
              cy="60"
              r="3.5"
              className={s.done ? 'fill-success-600 dark:fill-success-400' : 'fill-primary-500'}
            />
            <text
              x={s.x}
              y="88"
              textAnchor="middle"
              className="fill-slate-500 dark:fill-slate-400"
              fontSize="9"
            >
              {s.label}
            </text>
          </g>
        ))}
        <g className="animate-fade-in">
          <rect
            x="118"
            y="108"
            width="84"
            height="18"
            rx="9"
            className="fill-success-500/15 stroke-success-500"
            strokeWidth="1"
            transform="translate(-22 0)"
          />
          <text x="118" y="120" textAnchor="middle" className="fill-success-600" fontSize="8">
            SLA: 1.5 sa
          </text>
        </g>
      </svg>
      <figcaption className="mt-3 text-center text-[11px] text-slate-500 dark:text-slate-400">
        {caption}
      </figcaption>
    </figure>
  );
};

export const ModulesShowcase = () => {
  const { t } = useTranslation();
  const [activeMod, setActiveMod] = useState<ModuleType>('cad');

  const mods: ModuleDef[] = [
    {
      id: 'cad',
      icon: <DraftingCompass size={18} />,
      title: t('LandingPage.showcase.m1Title'),
      desc: t('LandingPage.showcase.m1Desc'),
      long: t('LandingPage.showcase.m1Long', {
        defaultValue:
          'Bayileriniz tarayıcıdan ölçü girer; motor profil kesim toleranslarını, açı kısıtlarını ve rüzgâr yükü statiğini anında doğrular. Hatalı çizim üretime düşmeden engellenir, her sipariş imalata hazır gelir.',
      }),
      color: 'text-primary-500 bg-primary-500/10 border-primary-500/20',
      accentBar: 'bg-primary-500',
      stats: [
        {
          value: '< %0.5',
          label: t('LandingPage.showcase.m1Stat1', { defaultValue: 'Çizim hata oranı' }),
        },
        {
          value: '3D',
          label: t('LandingPage.showcase.m1Stat2', { defaultValue: 'Açı yakalama' }),
        },
        {
          value: '6000 mm',
          label: t('LandingPage.showcase.m1Stat3', { defaultValue: 'Maks. panel açıklığı' }),
        },
      ],
      ui: (
        <CadSchematic
          caption={t('LandingPage.showcase.m1Caption', {
            defaultValue:
              'Kısıt tabanlı panel yakalama: açı, açıklık ve yük sınırları canlı kontrol edilir.',
          })}
        />
      ),
    },
    {
      id: 'mrp',
      icon: <Cpu size={18} />,
      title: t('LandingPage.showcase.m2Title'),
      desc: t('LandingPage.showcase.m2Desc'),
      long: t('LandingPage.showcase.m2Long', {
        defaultValue:
          'Yerleşim (nesting) algoritması cam ve sac levhalarını en az fire ile yerleştirir; temperleme fırını doluluğunu ve pişirme partilerini planlar. Hammadde maliyeti düşer, fırın bekleme süreleri kısalır.',
      }),
      color: 'text-accent-500 bg-accent-500/10 border-accent-500/20',
      accentBar: 'bg-accent-500',
      stats: [
        {
          value: '%98.8',
          label: t('LandingPage.showcase.m2Stat1', { defaultValue: 'Levha verimliliği' }),
        },
        {
          value: '%1.2',
          label: t('LandingPage.showcase.m2Stat2', { defaultValue: 'Hedef fire oranı' }),
        },
        {
          value: '%84',
          label: t('LandingPage.showcase.m2Stat3', { defaultValue: 'Fırın doluluğu' }),
        },
      ],
      ui: (
        <MrpSchematic
          caption={t('LandingPage.showcase.m2Caption', {
            defaultValue:
              'Levhaya optimum yerleşim ve fırın parti planlaması; boşluk fireye dönüşmeden değerlendirilir.',
          })}
        />
      ),
    },
    {
      id: 'b2b',
      icon: <Globe size={18} />,
      title: t('LandingPage.showcase.m3Title'),
      desc: t('LandingPage.showcase.m3Desc'),
      long: t('LandingPage.showcase.m3Long', {
        defaultValue:
          'Her bayi kendi fiyat listesi, cari limiti ve sipariş şablonlarıyla tek portaldan çalışır. Teklif, sipariş ve sevkiyat durumu gerçek zamanlı senkron kalır; bayi ağınız merkezden şeffaf yönetilir.',
      }),
      color: 'text-success-500 bg-success-500/10 border-success-500/20',
      accentBar: 'bg-success-500',
      stats: [
        {
          value: '7/24',
          label: t('LandingPage.showcase.m3Stat1', { defaultValue: 'Bayi self-servis' }),
        },
        {
          value: 'Canlı',
          label: t('LandingPage.showcase.m3Stat2', { defaultValue: 'Cari limit kontrolü' }),
        },
        {
          value: '∞',
          label: t('LandingPage.showcase.m3Stat3', { defaultValue: 'Ölçeklenebilir bayi' }),
        },
      ],
      ui: (
        <B2bSchematic
          caption={t('LandingPage.showcase.m3Caption', {
            defaultValue:
              'Merkez hub her bayiye fiyat ve stok yayar; sipariş ve cari hareketleri anında geri akar.',
          })}
        />
      ),
    },
    {
      id: 'finance',
      icon: <Coins size={18} />,
      title: t('LandingPage.showcase.m4Title'),
      desc: t('LandingPage.showcase.m4Desc'),
      long: t('LandingPage.showcase.m4Long', {
        defaultValue:
          'Sipariş ve fatura, çift taraflı yevmiye fişlerine otomatik dönüşür; mizan, gelir tablosu ve bilanço anlık hazır olur. KDV tevkifatı ve dönem kilitleriyle defterleriniz her zaman dengede kalır.',
      }),
      color: 'text-warning-500 bg-warning-500/10 border-warning-500/20',
      accentBar: 'bg-warning-500',
      stats: [
        {
          value: 'Otomatik',
          label: t('LandingPage.showcase.m4Stat1', { defaultValue: 'Yevmiye kaydı' }),
        },
        {
          value: 'Borç = Alacak',
          label: t('LandingPage.showcase.m4Stat2', { defaultValue: 'Her fiş dengeli' }),
        },
        {
          value: 'KDV',
          label: t('LandingPage.showcase.m4Stat3', { defaultValue: 'Tevkifat desteği' }),
        },
      ],
      ui: (
        <FinanceSchematic
          caption={t('LandingPage.showcase.m4Caption', {
            defaultValue:
              'Her yevmiye fişi borç ve alacak tarafıyla denkleşir; defter dengesizken kapanmaz.',
          })}
        />
      ),
    },
    {
      id: 'procure',
      icon: <ShoppingCart size={18} />,
      title: t('LandingPage.showcase.m5Title'),
      desc: t('LandingPage.showcase.m5Desc'),
      long: t('LandingPage.showcase.m5Long', {
        defaultValue:
          'İç talepten satın alma siparişine, tedarikçi tekliflerinin (RFQ) karşılaştırmasından barkodlu depo kabulüne kadar tüm zinciri yönetir. Minimum stok seviyeleri yeniden sipariş tetikler, en uygun teklif öne çıkar.',
      }),
      color: 'text-info-500 bg-info-500/10 border-info-500/20',
      accentBar: 'bg-info-500',
      stats: [
        {
          value: 'RFQ',
          label: t('LandingPage.showcase.m5Stat1', { defaultValue: 'Teklif karşılaştırma' }),
        },
        {
          value: 'Oto.',
          label: t('LandingPage.showcase.m5Stat2', { defaultValue: 'Yeniden sipariş tetiği' }),
        },
        {
          value: 'Barkod',
          label: t('LandingPage.showcase.m5Stat3', { defaultValue: 'Depo kabulü' }),
        },
      ],
      ui: (
        <ProcureSchematic
          caption={t('LandingPage.showcase.m5Caption', {
            defaultValue:
              'Tedarikçi teklifleri yan yana sıralanır; en uygun fiyat otomatik işaretlenir.',
          })}
        />
      ),
    },
    {
      id: 'service',
      icon: <Award size={18} />,
      title: t('LandingPage.showcase.m6Title'),
      desc: t('LandingPage.showcase.m6Desc'),
      long: t('LandingPage.showcase.m6Long', {
        defaultValue:
          'Kurulum sonrası dijital kabul formundan servis biletine, teknisyen atamasından iş emri kapanışına kadar saha süreçleri tek akışta izlenir. SLA süreleri ölçülür, garanti sözleşmeleri eksiksiz takip edilir.',
      }),
      color: 'text-primary-600 bg-primary-500/10 border-primary-500/20',
      accentBar: 'bg-primary-600',
      stats: [
        {
          value: 'SLA',
          label: t('LandingPage.showcase.m6Stat1', { defaultValue: 'Yanıt süresi takibi' }),
        },
        {
          value: 'Dijital',
          label: t('LandingPage.showcase.m6Stat2', { defaultValue: 'Kabul imzası' }),
        },
        {
          value: 'Garanti',
          label: t('LandingPage.showcase.m6Stat3', { defaultValue: 'Sözleşme izleme' }),
        },
      ],
      ui: (
        <ServiceSchematic
          caption={t('LandingPage.showcase.m6Caption', {
            defaultValue:
              'Talep, atama, onarım ve dijital kabul adımları SLA süresine karşı izlenir.',
          })}
        />
      ),
    },
  ];

  const currentMod = mods.find((m) => m.id === activeMod) || mods[0];

  return (
    <Section>
      <SectionHeader
        eyebrow={
          <>
            <Cpu size={12} />
            {t('LandingPage.showcase.badge', { defaultValue: 'MODÜLER MİMARİ' })}
          </>
        }
        title={t('LandingPage.showcase.title')}
        subtitle={t('LandingPage.showcase.subtitle')}
      />

      <div className="grid grid-cols-1 items-stretch gap-8 lg:grid-cols-12">
        <div
          role="tablist"
          aria-label={t('LandingPage.showcase.tablistLabel', {
            defaultValue: 'CoreAlign modülleri',
          })}
          className="flex flex-col gap-3 lg:col-span-5"
        >
          {mods.map((m) => {
            const isActive = m.id === activeMod;
            return (
              <button
                key={m.id}
                role="tab"
                type="button"
                aria-selected={isActive}
                onClick={() => setActiveMod(m.id)}
                className={`flex items-center gap-4 rounded-2xl border p-4 text-left transition-all duration-300 ${
                  isActive
                    ? 'border-primary-500 bg-primary-500/5 shadow-md shadow-primary-500/5 dark:border-primary-400 dark:bg-primary-400/10'
                    : 'border-slate-200/60 bg-white/40 hover:border-slate-300 dark:border-slate-800 dark:bg-slate-900/40 dark:hover:border-slate-700'
                }`}
              >
                <div
                  className={`rounded-xl border p-2.5 transition-colors ${isActive ? m.color : 'border-slate-200 bg-slate-50 text-slate-500 dark:border-slate-800 dark:bg-slate-900'}`}
                >
                  {m.icon}
                </div>
                <div className="min-w-0 flex-1">
                  <h3
                    className={`truncate text-sm font-bold ${isActive ? 'text-slate-900 dark:text-white' : 'text-slate-700 dark:text-slate-300'}`}
                  >
                    {m.title}
                  </h3>
                  <p className="mt-0.5 truncate text-[11px] text-slate-500 dark:text-slate-400">
                    {m.desc}
                  </p>
                </div>
              </button>
            );
          })}
        </div>

        <div
          role="tabpanel"
          key={currentMod.id}
          className="animate-fade-up flex flex-col justify-between overflow-hidden rounded-3xl border border-slate-200 bg-white p-8 shadow-xl lg:col-span-7 dark:border-slate-800/80 dark:bg-slate-950/65"
        >
          <div>
            <div className="flex items-center gap-3 border-b border-slate-100 pb-4 dark:border-slate-800">
              <div className={`rounded-xl border p-2.5 ${currentMod.color}`}>{currentMod.icon}</div>
              <div className="min-w-0">
                <h3 className="text-lg font-bold text-slate-900 dark:text-white">
                  {currentMod.title}
                </h3>
                <span className="text-[11px] font-medium text-slate-500 dark:text-slate-400">
                  {currentMod.desc}
                </span>
              </div>
            </div>

            <p className="mb-6 mt-6 text-sm leading-relaxed text-slate-600 dark:text-slate-400">
              {currentMod.long}
            </p>

            <div className="mb-6">
              <StatTiles stats={currentMod.stats} />
            </div>
          </div>

          <div className="rounded-2xl border border-slate-200/80 bg-slate-50/60 p-6 shadow-inner dark:border-slate-800/70 dark:bg-slate-950/70">
            <div className="mb-4 flex items-center gap-2">
              <span className={`h-2 w-2 animate-pulse-soft rounded-full ${currentMod.accentBar}`} />
              <span className="text-[10px] font-extrabold uppercase tracking-widest text-primary-500">
                {t('LandingPage.showcase.schematicLabel', {
                  defaultValue: 'Modül Şeması · Canlı Önizleme',
                })}
              </span>
            </div>
            {currentMod.ui}
          </div>
        </div>
      </div>

      <div className="mt-12 flex justify-start">
        <a
          href="#demo"
          className="inline-flex items-center gap-2 rounded-xl bg-primary-600 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-primary-500/30 transition hover:bg-primary-700 hover:shadow-primary-500/40"
        >
          {t('LandingPage.showcase.cta', {
            defaultValue: 'Tüm modülleri canlı demoda keşfedin',
          })}
          <ArrowRight size={16} />
        </a>
      </div>
    </Section>
  );
};
