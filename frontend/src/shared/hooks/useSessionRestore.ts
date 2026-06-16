import { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '../../app/hooks';
import { loginSuccess, sessionRestoreFailed } from '../../features/identity/identitySlice';
import { refreshAccessToken } from '../../features/identity/api/loginApi';
import { setAccessToken } from '../api/authInterceptor';
import { parseJwtClaims } from '../utils/parseJwt';

export function useSessionRestore(): boolean {
  const dispatch = useAppDispatch();
  const sessionChecked = useAppSelector((state) => state.identity.sessionChecked);

  useEffect(() => {
    if (sessionChecked) return;

    refreshAccessToken()
      .then((result) => {
        if (!result.success) {
          dispatch(sessionRestoreFailed());
          return;
        }

        setAccessToken(result.accessToken);
        const claims = parseJwtClaims(result.accessToken);

        if (claims) {
          dispatch(loginSuccess(claims));
        } else {
          dispatch(sessionRestoreFailed());
        }
      })
      .catch(() => {
        dispatch(sessionRestoreFailed());
      });
  }, [sessionChecked, dispatch]);

  return sessionChecked;
}
