-- Funkcja SQL do pobierania display name lub email użytkownika
-- Wykonaj to w Supabase SQL Editor

CREATE OR REPLACE FUNCTION get_user_email(user_uid UUID)
RETURNS TEXT AS $$
  SELECT COALESCE(
    raw_user_meta_data->>'display_name',
    email,
    user_uid::TEXT
  ) FROM auth.users WHERE id = user_uid;
$$ LANGUAGE SQL SECURITY DEFINER;

-- Uprawnienia - pozwól używać tej funkcji wszystkim zalogowanym użytkownikom
GRANT EXECUTE ON FUNCTION get_user_email(UUID) TO authenticated;
GRANT EXECUTE ON FUNCTION get_user_email(UUID) TO anon;
