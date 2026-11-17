import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../../api/client';
import { Avatar } from '../../components/common/Avatar';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import { ProjectRole } from '../../api/types';
import { Send, Trash2, Loader2 } from 'lucide-react';

interface CommentsListProps {
  issueId: string;
  projectId: string;
  userRole?: ProjectRole;
}

export const CommentsList: React.FC<CommentsListProps> = ({
  issueId,
  userRole,
}) => {
  const { user } = useAuth();
  const { showSuccess, showError } = useToast();
  const queryClient = useQueryClient();
  const [newComment, setNewComment] = useState('');

  const { data: comments, isLoading } = useQuery({
    queryKey: ['comments', issueId],
    queryFn: () => api.getComments(issueId),
    enabled: !!issueId,
  });

  const addCommentMutation = useMutation({
    mutationFn: (body: string) => api.addComment(issueId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments', issueId] });
      queryClient.invalidateQueries({ queryKey: ['issues'] });
      setNewComment('');
      showSuccess('Comment added');
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to post comment');
    },
  });

  const deleteCommentMutation = useMutation({
    mutationFn: (commentId: string) => api.deleteComment(commentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments', issueId] });
      queryClient.invalidateQueries({ queryKey: ['issues'] });
      showSuccess('Comment deleted');
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to delete comment');
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newComment.trim()) return;
    addCommentMutation.mutate(newComment.trim());
  };

  return (
    <div className="space-y-4 pt-4 border-t border-slate-100">
      <h4 className="text-sm font-bold text-slate-900">Activity & Comments</h4>

      {/* Add comment box */}
      <form onSubmit={handleSubmit} className="flex gap-2.5 items-start">
        {user && <Avatar name={user.name} size="sm" className="mt-1" />}
        <div className="flex-1">
          <textarea
            rows={2}
            value={newComment}
            onChange={(e) => setNewComment(e.target.value)}
            placeholder="Write a comment..."
            className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-brand-100 focus:border-brand-500"
          />
          <div className="mt-1.5 flex justify-end">
            <button
              type="submit"
              disabled={!newComment.trim() || addCommentMutation.isPending}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-white bg-brand-600 hover:bg-brand-700 rounded-lg shadow-sm transition disabled:opacity-50"
            >
              {addCommentMutation.isPending ? (
                <Loader2 className="w-3.5 h-3.5 animate-spin" />
              ) : (
                <Send className="w-3.5 h-3.5" />
              )}
              Comment
            </button>
          </div>
        </div>
      </form>

      {/* Comments List */}
      {isLoading ? (
        <div className="text-xs text-slate-400">Loading comments...</div>
      ) : !comments || comments.length === 0 ? (
        <p className="text-xs text-slate-400 italic py-2">No comments yet.</p>
      ) : (
        <div className="space-y-3 pt-2">
          {comments.map((comment) => {
            const canDelete =
              user?.id === comment.author.id || userRole === 'Admin';

            return (
              <div key={comment.id} className="flex items-start gap-2.5 group">
                <Avatar name={comment.author.name} size="sm" className="mt-0.5" />
                <div className="flex-1 bg-slate-50 p-3 rounded-2xl border border-slate-100">
                  <div className="flex items-center justify-between mb-1">
                    <span className="text-xs font-bold text-slate-800">
                      {comment.author.name}
                    </span>
                    <div className="flex items-center gap-2">
                      <span className="text-[11px] text-slate-400">
                        {new Date(comment.createdAt).toLocaleTimeString([], {
                          hour: '2-digit',
                          minute: '2-digit',
                        })}
                      </span>
                      {canDelete && (
                        <button
                          onClick={() => deleteCommentMutation.mutate(comment.id)}
                          className="opacity-0 group-hover:opacity-100 text-slate-400 hover:text-rose-600 transition"
                          title="Delete comment"
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      )}
                    </div>
                  </div>
                  <p className="text-xs text-slate-600 whitespace-pre-wrap">{comment.body}</p>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
