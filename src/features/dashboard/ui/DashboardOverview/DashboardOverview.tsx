import React from 'react';
import { useTranslation } from 'react-i18next';
import { Users, DollarSign, ShoppingCart } from 'lucide-react';
import { StatCard, StatCardProps } from '@/shared/ui/Card/StatCard';
import { AreaChartCard } from '@/shared/ui/Chart/AreaChartCard';
import { DataTableCard } from '@/shared/ui/Table/DataTableCard';
import { FuturisticCard } from '@/shared/ui/Card/FuturisticCard';

const data = [
    { name: 'Jan', revenue: 4000 },
    { name: 'Feb', revenue: 3000 },
    { name: 'Mar', revenue: 2000 },
    { name: 'Apr', revenue: 2780 },
    { name: 'May', revenue: 1890 },
    { name: 'Jun', revenue: 2390 },
    { name: 'Jul', revenue: 3490 },
];

const tableData = [
    { id: 'ORD-001', customer: 'Acme Corp', amount: '$1,200.00', status: 'Completed', date: '2023-10-25' },
    { id: 'ORD-002', customer: 'Global Tech', amount: '$3,450.00', status: 'Pending', date: '2023-10-26' },
    { id: 'ORD-003', customer: 'Stark Ind.', amount: '$850.00', status: 'Processing', date: '2023-10-26' },
    { id: 'ORD-004', customer: 'Wayne Ent.', amount: '$5,000.00', status: 'Completed', date: '2023-10-27' },
    { id: 'ORD-005', customer: 'Oscorp', amount: '$2,100.00', status: 'Cancelled', date: '2023-10-28' },
];

export const DashboardOverview: React.FC = () => {
    const { t } = useTranslation();

    const stats: StatCardProps[] = [
        {
            name: t('dashboard.total_revenue'),
            value: '$45,231.89',
            change: '+20.1%',
            trend: 'up',
            icon: DollarSign,
            color: 'from-blue-500 to-indigo-500',
        },
        {
            name: t('dashboard.active_users'),
            value: '2,338',
            change: '+15.1%',
            trend: 'up',
            icon: Users,
            color: 'from-emerald-400 to-teal-500',
        },
        {
            name: t('dashboard.new_orders'),
            value: '1,234',
            change: '-3.2%',
            trend: 'down',
            icon: ShoppingCart,
            color: 'from-orange-400 to-red-500',
        },
    ];

    return (
        <div className="space-y-[5px] pb-[5px] relative">

            {/* Top Row: Stats Grid */}
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-[5px]">
                {stats.map((stat) => (
                    <StatCard key={stat.name} {...stat} />
                ))}
            </div>

            {/* Middle Row: Chart + Futuristic Card */}
            <div className="grid grid-cols-1 lg:grid-cols-4 gap-[5px]">
                <div className="lg:col-span-3">
                    <AreaChartCard title={t('dashboard.revenue_analytics')} data={data} />
                </div>
                <div className="lg:col-span-1 hidden lg:block">
                    <FuturisticCard />
                </div>
            </div>

            {/* Bottom Row: Data Table */}
            <DataTableCard data={tableData} />

        </div>
    );
};
