// Provides the signed-in user (non-secret profile) to the whole app via the
// root layout. Authenticated enforcement happens per-route in +layout.server.ts.
export const load = ({ locals }) => {
	return { user: locals.user };
};
