"""Keep legacy-engine effect descriptions aligned with the maintained card database."""

def apply_description_fixes(scripts):
    name = 'c92895501.lua'
    body = scripts[name]
    # Heavy Borger uses separate draw/damage effects in this engine. The newer
    # database inserted a combined-effect heading ahead of their descriptions.
    body = body.replace(b'e2:SetDescription(aux.Stringid(92895501,1))',
                        b'e2:SetDescription(aux.Stringid(92895501,2))')
    body = body.replace(b'e3:SetDescription(aux.Stringid(92895501,2))',
                        b'e3:SetDescription(aux.Stringid(92895501,3))')
    scripts[name] = body
