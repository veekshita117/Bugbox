import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { api } from '../../api/client';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { TrendingDown } from 'lucide-react';

interface BurndownChartProps {
  sprintId: string;
}

export const BurndownChart: React.FC<BurndownChartProps> = ({ sprintId }) => {
  const { data: points, isLoading, isError } = useQuery({
    queryKey: ['burndown', sprintId],
    queryFn: () => api.getBurndown(sprintId),
    enabled: !!sprintId,
  });

  if (isLoading) {
    return (
      <div className="h-64 flex items-center justify-center bg-slate-50 rounded-2xl animate-pulse text-xs text-slate-400">
        Generating burndown curve...
      </div>
    );
  }

  if (isError || !points || points.length === 0) {
    return (
      <div className="h-64 flex items-center justify-center bg-slate-50 rounded-2xl text-xs text-slate-400">
        No burndown data available for this sprint.
      </div>
    );
  }

  // Format dates for display on X-axis (e.g., "Nov 12")
  const chartData = points.map((p) => {
    const d = new Date(p.date + 'T00:00:00');
    return {
      ...p,
      displayDate: d.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }),
    };
  });

  return (
    <div className="bg-white p-5 rounded-3xl border border-slate-200/80 shadow-sm space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <div className="p-2 rounded-xl bg-indigo-50 text-indigo-600">
            <TrendingDown className="w-4 h-4" />
          </div>
          <div>
            <h4 className="text-sm font-bold text-slate-900">Sprint Burndown</h4>
            <p className="text-xs text-slate-400">
              Remaining issues vs ideal trajectory
            </p>
          </div>
        </div>
      </div>

      <div className="h-64 w-full pt-2">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={chartData} margin={{ top: 10, right: 20, left: -20, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" vertical={false} />
            <XAxis
              dataKey="displayDate"
              tick={{ fontSize: 11, fill: '#94a3b8' }}
              axisLine={{ stroke: '#e2e8f0' }}
              tickLine={false}
            />
            <YAxis
              allowDecimals={false}
              tick={{ fontSize: 11, fill: '#94a3b8' }}
              axisLine={{ stroke: '#e2e8f0' }}
              tickLine={false}
            />
            <Tooltip
              contentStyle={{
                backgroundColor: '#ffffff',
                borderRadius: '12px',
                border: '1px solid #e2e8f0',
                boxShadow: '0 10px 15px -3px rgba(0, 0, 0, 0.1)',
                fontSize: '12px',
              }}
            />
            <Legend
              wrapperStyle={{ fontSize: '12px', paddingTop: '10px' }}
              iconType="circle"
            />
            <Line
              type="monotone"
              dataKey="ideal"
              name="Ideal Guide"
              stroke="#94a3b8"
              strokeWidth={2}
              strokeDasharray="4 4"
              dot={false}
            />
            <Line
              type="monotone"
              dataKey="remaining"
              name="Actual Remaining"
              stroke="#6366f1"
              strokeWidth={2.5}
              activeDot={{ r: 6, fill: '#4f46e5' }}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};
