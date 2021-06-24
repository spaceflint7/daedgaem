
using System;
using android.view;
using android.widget;

namespace com.spaceflint
{

    public sealed class GameSelectView : LinearLayout
    {

        // --------------------------------------------------------------------
        // constructor

        public GameSelectView (Activity activity, Action<Type> onGameSelected)
            : base(activity)
        {
            this.activity = activity;

            setOrientation(LinearLayout.VERTICAL);

            TitleText();
            TextLine("Emulator for five DOS games from 1984", 14,
                                     android.graphics.Color.GREEN);

            var buttons = ImageButton(null, "Info",
                ((View.OnClickListener.Delegate) ((_) => ShowInfo())));

            foreach (var cls in GameClasses)
            {
                buttons = ImageButton(buttons, cls.Name.Substring(4),
                    ((View.OnClickListener.Delegate) ((_) => onGameSelected(cls))));

            }

            TextLine("", 6, android.graphics.Color.BLACK);
            TextLine("All rights to these fives games belong to their respective owners", 8,
                     android.graphics.Color.GRAY);
        }

        // --------------------------------------------------------------------
        // GameTypeFromSavedInstance

        public static Type GameTypeFromSavedInstance (android.os.Bundle savedInstanceState)
        {
            var name = savedInstanceState?.getCharSequence("game")?.ToString();
            if (name != null)
            {
                name = $"Game{name}";
                foreach (var cls in GameClasses)
                {
                    if (cls.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return cls;
                }
            }
            return null;
        }

        // --------------------------------------------------------------------

        private void TextLine (string str, int ptsize, int color)
        {
            var textView = new TextView(activity);
            textView.setText((java.lang.CharSequence) (object) str);
            textView.setTextColor(color);
            textView.setGravity(android.view.Gravity.CENTER_HORIZONTAL);
            textView.setMaxLines(1);

            textView.setTextSize(android.util.TypedValue.COMPLEX_UNIT_PT,
                                 (float) ptsize);

            textView.setLayoutParams(new LinearLayout.LayoutParams(
                                        LinearLayout.LayoutParams.FILL_PARENT,
                                        LinearLayout.LayoutParams.WRAP_CONTENT)
                                            { weight = 0.7f });

            addView(textView);
        }

        // --------------------------------------------------------------------

        private void TitleText ()
        {
            var container = new LinearLayout(activity);
            container.setOrientation(LinearLayout.HORIZONTAL);

            container.setLayoutParams(new LinearLayout.LayoutParams(
                                        LinearLayout.LayoutParams.WRAP_CONTENT,
                                        LinearLayout.LayoutParams.WRAP_CONTENT)
                                            { gravity = Gravity.CENTER_HORIZONTAL,
                                              weight = 0.7f });

            var colors = new int[]
            {
                android.graphics.Color.BLUE,
                android.graphics.Color.CYAN,
                android.graphics.Color.GREEN,
                android.graphics.Color.MAGENTA,
                android.graphics.Color.RED,
                android.graphics.Color.WHITE,
                android.graphics.Color.YELLOW,
            };
            int colorIndex = 0;

            foreach (var ch in "Daed Gaem")
            {
                var textView = new TextView(activity);
                textView.setText((java.lang.CharSequence) (object) $" {ch.ToString()} ");
                textView.setTextColor(colors[(++colorIndex) % colors.Length]);
                textView.setMaxLines(1);

                textView.setTextSize(android.util.TypedValue.COMPLEX_UNIT_PT, 22f);

                textView.setLayoutParams(new LinearLayout.LayoutParams(
                                            LinearLayout.LayoutParams.WRAP_CONTENT,
                                            LinearLayout.LayoutParams.FILL_PARENT));
                container.addView(textView);
            }

            addView(container);

            postDelayed( ((java.lang.Runnable.Delegate)
                    (() => TitleTextAnimation(container, colors, 1)))
                                .AsInterface(), 300);
        }

        // --------------------------------------------------------------------

        private void TitleTextAnimation (LinearLayout container,
                                         int[] colors, int colorIndex)
        {
            for (int i = container.getChildCount(); --i > 0; )
            {
                if (container.getChildAt(i) is TextView textChild)
                    textChild.setTextColor(colors[(colorIndex + i) % colors.Length]);
            }

            postDelayed( ((java.lang.Runnable.Delegate)
                    (() => TitleTextAnimation(container, colors, colorIndex + 1)))
                                .AsInterface(), 300);
        }

        // --------------------------------------------------------------------

        private LinearLayout ImageButton (LinearLayout container, string name,
                                          View.OnClickListener.Delegate dlg)
        {
            var stream = activity.getAssets().open($"{name}.png");

            var imageView = new ImageView(activity);
            imageView.setImageBitmap(
                    android.graphics.BitmapFactory.decodeStream(stream));
            imageView.setScaleType(ImageView.ScaleType.FIT_XY);

            imageView.setLayoutParams(new LinearLayout.LayoutParams(
                                        LinearLayout.LayoutParams.WRAP_CONTENT,
                                        LinearLayout.LayoutParams.FILL_PARENT)
                                            { weight = 0.4f });

            imageView.setClickable(true);
            imageView.setOnClickListener(dlg.AsInterface());

            if (container is null)
            {
                container = new LinearLayout(activity);
                container.setOrientation(LinearLayout.HORIZONTAL);

                container.setLayoutParams(new LinearLayout.LayoutParams(
                                            LinearLayout.LayoutParams.FILL_PARENT,
                                            LinearLayout.LayoutParams.WRAP_CONTENT)
                                                { weight = 0.9f });
            }

            container.addView(imageView);

            if (container.getChildCount() == 2)
            {
                TextLine("", 2, android.graphics.Color.BLACK);
                addView(container);
                container = null;
            }

            return container;
        }

        // --------------------------------------------------------------------

        private void ShowInfo ()
        {
            string url = "https://github.com/spaceflint7/daedgaem";
            activity.startActivity(
                new android.content.Intent(android.content.Intent.ACTION_VIEW,
                    android.net.Uri.parse(url)));
        }

        // --------------------------------------------------------------------
        // list of game classes.  this prevents these classes from
        // being discarded due to filtering in Bluebonnet PruneMerge

        private static Type[] GameClasses =
        {
            typeof(GameAlleyCat),
            typeof(GameBouncingBabies),
            typeof(GameBuckRogers),
            typeof(GameHardHatMack),
            typeof(GamePitStop)
        };

        // --------------------------------------------------------------------

        Activity activity;

    }
}
